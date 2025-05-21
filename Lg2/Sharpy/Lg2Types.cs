using System.Runtime.InteropServices;
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

            throw new Lg2Exception(
                errorCode,
                $"Lg2 Error: {errorCode}: {lastError->klass}: {message}"
            );
        }
        else
        {
            throw new Lg2Exception(GIT_ERROR, $"Lg2 Error: unknown error code {code} encountered");
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

static unsafe class Lg2StrArrayExtensions { }

internal unsafe class Lg2Config : SafeHandle
{
    internal Lg2Config(git_config* pConfig)
        : base(nint.Zero, true)
    {
        handle = (nint)pConfig;
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            git_config_free((git_config*)handle);
        }
        return true;
    }

    internal git_config* Ptr => (git_config*)handle;

    internal static Lg2Config New()
    {
        git_config* pConfig = null;
        var rc = git_config_new(&pConfig);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Config(pConfig);
    }
}

internal static unsafe class Lg2ConfigExtensions
{
    static void EnsureValidConfig(Lg2Config config)
    {
        if (config.IsInvalid)
        {
            throw new ArgumentException($"Invalid {nameof(config)}");
        }
    }

    internal static void SetString(this Lg2Config config, string name, string value)
    {
        EnsureValidConfig(config);

        using var u8Name = new Lg2Utf8String(name);
        using var u8Value = new Lg2Utf8String(value);

        var rc = git_config_set_string(config.Ptr, u8Name.Ptr, u8Value.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);
    }
}

internal unsafe class Lg2RevWalk : SafeHandle
{
    internal Lg2RevWalk(git_revwalk* pRevWalk)
        : base(default, true)
    {
        handle = (nint)pRevWalk;
    }

    public override bool IsInvalid => handle == default;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            git_revwalk_free((git_revwalk*)handle);
            handle = default;
        }

        return true;
    }

    internal git_revwalk* Ptr => (git_revwalk*)handle;
}

internal static unsafe class Lg2RevWalkExtensions
{
    static void EnsureValidRevWalk(Lg2RevWalk revWalk)
    {
        if (revWalk.IsInvalid)
        {
            throw new ArgumentException($"Invalid {nameof(revWalk)}");
        }
    }

    internal static void PushRef(this Lg2RevWalk revWalk, string refName)
    {
        EnsureValidRevWalk(revWalk);

        using var u8RefName = new Lg2Utf8String(refName);
        var rc = git_revwalk_push_ref(revWalk.Ptr, u8RefName.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);
    }

    internal static bool Next(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        EnsureValidRevWalk(revWalk);

        var rc = (int)GIT_OK;
        fixed (git_oid* pOid = &oid._raw)
        {
            rc = git_revwalk_next(pOid, revWalk.Ptr);
        }

        if (rc == (int)GIT_ITEROVER)
        {
            return false;
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return true;
    }

    internal static void Hide(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        EnsureValidRevWalk(revWalk);

        var rc = (int)GIT_OK;
        fixed (git_oid* pOid = &oid._raw)
        {
            rc = git_revwalk_hide(revWalk.Ptr, pOid);
        }
        Lg2Exception.RaiseIfNotOk(rc);
    }

    internal static void AddHideCallback(this Lg2RevWalk revWalk)
    {
        EnsureValidRevWalk(revWalk);

        // TODO
        // git_revwalk_add_hide_cb();

        throw new NotImplementedException();
    }
}

internal unsafe ref struct Lg2Oid
{
    internal git_oid _raw;

    public override string ToString()
    {
        var buf = stackalloc sbyte[GIT_OID_MAX_HEXSIZE + 1];
        var rc = (int)GIT_OK;
        fixed (git_oid* pRaw = &_raw)
        {
            rc = git_oid_fmt(buf, pRaw);
        }
        Lg2Exception.RaiseIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)buf) ?? string.Empty;

        return result;
    }
}

internal unsafe class Lg2Odb : SafeHandle
{
    Lg2Odb(git_odb* pOdb)
        : base(default, true)
    {
        handle = (nint)pOdb;
    }

    public override bool IsInvalid => handle == default;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            git_odb_free((git_odb*)handle);
            handle = default;
        }

        return true;
    }

    internal static Lg2Odb Open(string objectsDir)
    {
        var u8ObjectsDir = new Lg2Utf8String(objectsDir);

        git_odb* pOdb = null;
        var rc = git_odb_open(&pOdb, u8ObjectsDir.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Odb(pOdb);
    }

    internal static Lg2Odb New()
    {
        git_odb* pOdb = null;
        var rc = git_odb_new(&pOdb);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Odb(pOdb);
    }
}

internal static unsafe class Lg2OdbExtensions
{
    static void EnsureValidOdb(Lg2Odb odb)
    {
        if (odb.IsInvalid)
        {
            throw new ArgumentException($"Invalid {nameof(odb)}");
        }
    }
}

internal unsafe class Lg2Utf8String : SafeHandle
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
