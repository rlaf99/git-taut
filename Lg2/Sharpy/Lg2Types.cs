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

    bool initialized = false;

    public Lg2Global() { }

    public void Init()
    {
        var rc = git_libgit2_init();
        Lg2Exception.ThrowIfNotOk(rc);
        initialized = true;
    }

    public void Dispose()
    {
        if (initialized)
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
        var stringsSize = Marshal.SizeOf(typeof(sbyte*)) * input.Count;

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
    internal static List<string> ToList(ref readonly this git_strarray strarray)
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
            set => throw new NotSupportedException();
        }

        public override void Flush() { } // do nothing

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
}

public unsafe ref struct Lg2RawData
{
    internal nint Ptr;
    internal long Len;
}

public static unsafe class Lg2RawDataExtensions
{
    public static ReadOnlySpan<byte> AsReadOnlySpan(this Lg2RawData rawData)
    {
        return new((void*)rawData.Ptr, (int)rawData.Len);
    }

    public static bool IsInvalid(this Lg2RawData rawData)
    {
        if (rawData.Ptr == default || rawData.Len <= 0)
        {
            return true;
        }

        return false;
    }

    public static bool IsBinary(this Lg2RawData rawData)
    {
        if (rawData.IsInvalid())
        {
            throw new InvalidOperationException($"Invalid {nameof(rawData)}");
        }

        var result = git_blob_data_is_binary((sbyte*)rawData.Ptr, (nuint)rawData.Len);

        return result != 0;
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
