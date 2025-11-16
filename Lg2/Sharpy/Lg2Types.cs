using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public class Lg2Exception : Exception
{
    readonly git_error_code _errorCode = GIT_OK;

    internal Lg2Exception(git_error_code errorCode, string? message = null)
        : base(message)
    {
        _errorCode = errorCode;
    }

    internal Lg2Exception(string message)
        : base(message) { }

    internal static unsafe void ThrowIfNotOk(int code)
    {
        if (code >= 0)
        {
            return;
        }

        if (Enum.IsDefined(typeof(git_error_code), code))
        {
            var errorCode = (git_error_code)code;
            var lastError = git_error_last();
            var message = Marshal.PtrToStringUTF8((nint)lastError->message);

            throw new Lg2Exception(errorCode, $"{errorCode}: {lastError->klass}: {message}");
        }
        else
        {
            throw new Lg2Exception(GIT_ERROR, $"unknown error code {code} encountered");
        }
    }
}

public sealed class Lg2Global : IDisposable
{
    public static readonly string Version = Encoding.UTF8.GetString(LIBGIT2_VERSION);

    bool _initialized = false;

    public Lg2Global() { }

    public void Init()
    {
        if (_initialized)
        {
            throw new InvalidOperationException($"Already initialized");
        }
        _initialized = true;

        var rc = git_libgit2_init();
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public void Dispose()
    {
        if (_initialized)
        {
            var rc = git_libgit2_shutdown();
            _ = rc; // ignore it for now
        }
    }
}

public sealed unsafe class Lg2StrArray : IDisposable
{
    internal git_strarray Raw;

    bool _disposed = false;

    ~Lg2StrArray() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (disposing) { } // for managed resource

        FreeUnmanaged();
    }

    void FreeUnmanaged()
    {
        if (Raw.strings != null)
        {
            for (nuint i = 0; i < Raw.count; i++)
            {
                Marshal.FreeCoTaskMem((nint)Raw.strings[i]);
            }

            Marshal.FreeCoTaskMem((nint)Raw.strings);
        }

        Raw = new();
    }

    public static Lg2StrArray FromList(List<string> input)
    {
        var stringsSize = sizeof(sbyte*) * input.Count;

        git_strarray strarray = new()
        {
            strings = (sbyte**)Marshal.AllocCoTaskMem(stringsSize),
            count = (nuint)input.Count,
        };

        for (int i = 0; i < input.Count; i++)
        {
            strarray.strings[i] = (sbyte*)Marshal.StringToCoTaskMemUTF8(input[i]);
        }

        Lg2StrArray result = new() { Raw = strarray };

        return result;
    }
}

internal static unsafe partial class RawStrArrayExtensions
{
    internal static List<string> ToList(this scoped ref git_strarray strarray)
    {
        List<string> result = [];

        for (nuint i = 0; i < strarray.count; i++)
        {
            var entry = Marshal.PtrToStringUTF8((nint)strarray.strings[i]);
            result.Add(entry ?? string.Empty);
        }

        return result;
    }
}

public sealed unsafe class Lg2Buf : IDisposable
{
    internal git_buf Raw;

    internal Lg2Buf(git_buf buf)
    {
        Raw = buf;
    }

    public long Length => (long)Raw.size;

    public ReadStream NewReadStream() => new(this);

    bool _isDiposed;

    void Dispose(bool disposing)
    {
        if (_isDiposed)
        {
            return;
        }
        _isDiposed = true;

        if (disposing) { }

        fixed (git_buf* ptr = &Raw)
        {
            git_buf_dispose(ptr);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Lg2Buf() => Dispose(false);

    public class ReadStream(Lg2Buf sourceBuf) : Stream
    {
        long _totalRead;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => sourceBuf.Length;

        public override long Position
        {
            get => _totalRead;
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, sourceBuf.Length);
                _totalRead = value;
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var dataRead = 0;

            var target = buffer.AsSpan(offset, count);

            while (_totalRead < sourceBuf.Length && dataRead < target.Length)
            {
                target[dataRead++] = (byte)sourceBuf.Raw.ptr[_totalRead++];
            }

            return dataRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}

public static unsafe class Lg2BufExtensions
{
    public static ReadOnlySpan<byte> GetRawData(this Lg2Buf buf)
    {
        return new(buf.Raw.ptr, (int)buf.Raw.size);
    }

    public static void DumpTo(this Lg2Buf buf, Stream targetStream)
    {
        targetStream.Write(buf.GetRawData());
    }
}

public unsafe partial struct Lg2Trace
{
    public delegate void TraceOutput(string message);

    static TraceOutput? _traceOutput = null;

    public static void SetTraceOutput(TraceOutput? traceOutput)
    {
        _traceOutput = traceOutput;

        if (_traceOutput is not null)
        {
            var rc = git_trace_set(git_trace_level_t.GIT_TRACE_TRACE, &OutputTrace);
            Lg2Exception.ThrowIfNotOk(rc);

            _traceOutput($"Set trace output for {nameof(Lg2Trace)}");
        }
        else
        {
            var rc = git_trace_set(git_trace_level_t.GIT_TRACE_NONE, null);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static void OutputTrace(git_trace_level_t level, sbyte* msg)
    {
        if (_traceOutput is not null)
        {
            var message = Marshal.PtrToStringUTF8((nint)msg);
            if (message is not null)
            {
                _traceOutput(message);
            }
        }
    }
}

public unsafe class Lg2Utf8String : SafeHandle
{
    internal Lg2Utf8String(string source)
        : base(nint.Zero, true)
    {
        handle = Marshal.StringToCoTaskMemUTF8(source);
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            Marshal.FreeCoTaskMem(handle);
            handle = default;
        }

        return true;
    }

    internal sbyte* Ptr => (sbyte*)handle;
}
