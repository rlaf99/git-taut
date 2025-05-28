using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public class Lg2Exception : Exception
{
    git_error_code _errorCode = GIT_OK;

    internal Lg2Exception(git_error_code errorCode, string? message = null)
        : base(message)
    {
        _errorCode = errorCode;
    }

    internal Lg2Exception(string message)
        : base(message) { }

    internal static unsafe void RaiseIfNotOk(int code)
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

public ref struct Lg2Global : IDisposable
{
    public static readonly string Version = Encoding.UTF8.GetString(LIBGIT2_VERSION);

    bool initialized = false;

    public Lg2Global() { }

    public void Init()
    {
        var rc = git_libgit2_init();
        Lg2Exception.RaiseIfNotOk(rc);
        initialized = true;
    }

    public readonly void Dispose()
    {
        if (initialized)
        {
            var rc = git_libgit2_shutdown();
            _ = rc; // ignore it for now
        }
    }
}

internal sealed unsafe class Lg2StrArray : IDisposable
{
    git_strarray _strArray = new();

    bool _nativeAllocated = false;

    bool _disposed = false;

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

        if (_nativeAllocated)
        {
            fixed (git_strarray* pStrArray = &_strArray)
            {
                git_strarray_dispose(pStrArray);
            }
        }
        else
        {
            FreeUnmanaged();
        }
    }

    void FreeUnmanaged()
    {
        if (_strArray.strings != null)
        {
            for (nuint i = 0; i < _strArray.count; i++)
            {
                Marshal.FreeCoTaskMem((nint)_strArray.strings[i]);
            }

            Marshal.FreeCoTaskMem((nint)_strArray.strings);
        }

        _strArray.count = 0;
    }

    ~Lg2StrArray() => Dispose(false);

    internal static Lg2StrArray FromNative(git_strarray strArray)
    {
        var lg2StrArray = new Lg2StrArray { _strArray = strArray, _nativeAllocated = true, };

        return lg2StrArray;
    }

    internal List<string> ToList()
    {
        List<string> result = [];

        for (nuint i = 0; i < _strArray.count; i++)
        {
            var entry = Marshal.PtrToStringUTF8((nint)_strArray.strings[i]);
            result.Add(entry ?? string.Empty);
        }

        return result;
    }
}

public unsafe ref struct Lg2RawData
{
    internal void* Ptr;
    internal nuint Len;
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
            Lg2Exception.RaiseIfNotOk(rc);

            _traceOutput($"Set trace output for {nameof(Lg2Trace)}");
        }
        else
        {
            var rc = git_trace_set(git_trace_level_t.GIT_TRACE_NONE, null);
            Lg2Exception.RaiseIfNotOk(rc);
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

    public static implicit operator sbyte*(Lg2Utf8String str) => (sbyte*)str.handle;

    public static implicit operator Lg2Utf8String(string str) => new(str);
}

public unsafe class Lg2Repository
    : NativeSafePointer<Lg2Repository, git_repository>,
        INativeRelease<git_repository>
{
    public Lg2Repository()
        : base(null) { }

    internal Lg2Repository(git_repository* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_repository* pNative)
    {
        git_repository_free(pNative);
    }

    static git_repository* OpenRaw(string repoPath)
    {
        using var u8Path = new Lg2Utf8String(repoPath);

        git_repository* pRepo;
        var rc = git_repository_open(&pRepo, u8Path.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return pRepo;
    }

    public void Open(string repoPath)
    {
        if (IsInvalid == false)
        {
            throw new InvalidOperationException($"{nameof(Lg2Repository)} is already opened");
        }

        var pRepo = OpenRaw(repoPath);

        SetHandle((nint)pRepo);
    }

    public static Lg2Repository New(string repoPath)
    {
        var u8Path = new Lg2Utf8String(repoPath);

        git_repository* pRepo;
        var rc = git_repository_open(&pRepo, u8Path.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Repository(pRepo);
    }
}

public static unsafe partial class Lg2RepositoryExtensions
{
    public static bool IsBare(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var val = git_repository_is_bare(repo.Ptr);
        return val != 0;
    }

    public static string GetPath(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var pPath = git_repository_path(repo.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pPath) ?? string.Empty;

        return result;
    }
}
