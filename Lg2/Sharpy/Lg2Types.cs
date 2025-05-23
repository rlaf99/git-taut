using System.Runtime.CompilerServices;
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

public unsafe interface IReleaseNative<TNative>
    where TNative : unmanaged
{
    static abstract void ReleaseNative(TNative* pNative);
}

public abstract unsafe class SafeNativePointer<TDerived, TNative> : SafeHandle
    where TNative : unmanaged
    where TDerived : IReleaseNative<TNative>
{
    public delegate void Release(TNative* pNative);

    internal SafeNativePointer(TNative* pNative)
        : base(default, true)
    {
        handle = (nint)pNative;
    }

    public override bool IsInvalid => handle == default;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            TDerived.ReleaseNative((TNative*)handle);
            handle = default;
        }

        return true;
    }

    internal TNative* Ptr => (TNative*)handle;

    public void EnsureValid()
    {
        if (IsInvalid)
        {
            throw new InvalidOperationException($"The instance of {nameof(TDerived)} is not valid");
        }
    }
}

public sealed unsafe class Lg2Config
    : SafeNativePointer<Lg2Config, git_config>,
        IReleaseNative<git_config>
{
    internal Lg2Config(git_config* pNative)
        : base(pNative) { }

    public static void ReleaseNative(git_config* pNative)
    {
        git_config_free(pNative);
    }

    public static Lg2Config New()
    {
        git_config* pConfig = null;
        var rc = git_config_new(&pConfig);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Config(pConfig);
    }
}

public static unsafe class Lg2ConfigExtensions
{
    public static void SetString(this Lg2Config config, string name, string value)
    {
        config.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);
        using var u8Value = new Lg2Utf8String(value);

        var rc = git_config_set_string(config.Ptr, u8Name.Ptr, u8Value.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);
    }
}

public unsafe class Lg2RevWalk
    : SafeNativePointer<Lg2RevWalk, git_revwalk>,
        IReleaseNative<git_revwalk>
{
    internal Lg2RevWalk(git_revwalk* pNative)
        : base(pNative) { }

    public static void ReleaseNative(git_revwalk* pNative)
    {
        git_revwalk_free(pNative);
    }
}

public static unsafe class Lg2RevWalkExtensions
{
    public static void PushRef(this Lg2RevWalk revWalk, string refName)
    {
        revWalk.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);
        var rc = git_revwalk_push_ref(revWalk.Ptr, u8RefName.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);
    }

    public static bool Next(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        revWalk.EnsureValid();

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
        revWalk.EnsureValid();

        var rc = (int)GIT_OK;
        fixed (git_oid* pOid = &oid._raw)
        {
            rc = git_revwalk_hide(revWalk.Ptr, pOid);
        }
        Lg2Exception.RaiseIfNotOk(rc);
    }

    internal static void AddHideCallback(this Lg2RevWalk revWalk)
    {
        revWalk.EnsureValid();

        // TODO
        // git_revwalk_add_hide_cb();

        throw new NotImplementedException();
    }
}

public readonly unsafe ref struct Lg2OidPlainRef
{
    internal readonly git_oid* Ptr;

    internal Lg2OidPlainRef(git_oid* pOid)
    {
        Ptr = pOid;
    }
}

public interface ILg2WithOid
{
    Lg2OidPlainRef GetOidPlainRef();
}

public static unsafe class Lg2WithOidExtensions
{
    public static string GetOidString(this ILg2WithOid oid)
    {
        var oidRef = oid.GetOidPlainRef();
        return Lg2Oid.ToString(oidRef.Ptr);
    }
}

public unsafe class Lg2OidOwnedRef<TOwner> : ILg2WithOid
    where TOwner : class
{
    readonly WeakReference<TOwner> _ownerWeakRef;
    readonly git_oid* _pOid;

    internal Lg2OidOwnedRef(TOwner owner, git_oid* pOid)
    {
        _ownerWeakRef = new WeakReference<TOwner>(owner);
        _pOid = pOid;
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        return new Lg2OidPlainRef(_pOid);
    }

    void EnsureValid()
    {
        if (_ownerWeakRef.TryGetTarget(out _) == false)
        {
            throw new InvalidOperationException($"The instance of {nameof(TOwner)} is not valid");
        }
    }
}

public unsafe ref struct Lg2Oid
{
    internal git_oid _raw;

    public override string ToString()
    {
        fixed (git_oid* pOid = &_raw)
        {
            return ToString(pOid);
        }
    }

    public static string ToString(git_oid* pOid)
    {
        var buf = stackalloc sbyte[GIT_OID_MAX_HEXSIZE + 1];
        var rc = (int)GIT_OK;
        rc = git_oid_fmt(buf, pOid);
        Lg2Exception.RaiseIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)buf) ?? string.Empty;

        return result;
    }
}

public unsafe ref struct Lg2RawData
{
    internal void* Ptr;
    internal nuint Len;
}

public unsafe class Lg2OdbObject
    : SafeNativePointer<Lg2OdbObject, git_odb_object>,
        IReleaseNative<git_odb_object>,
        ILg2WithOid
{
    internal Lg2OdbObject(git_odb_object* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_odb_object* pNative)
    {
        git_odb_object_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_odb_object_id(Ptr);

        return new(pOid);
    }
}

public static unsafe class Lg2OdbObjectExtensions
{
    public static Lg2RawData GetRawData(this Lg2OdbObject odbObject)
    {
        odbObject.EnsureValid();

        Lg2RawData rawData =
            new()
            {
                Ptr = git_odb_object_data(odbObject.Ptr),
                Len = git_odb_object_size(odbObject.Ptr),
            };

        return rawData;
    }

    public static Lg2ObjectType GetObjectType(this Lg2OdbObject odbObject)
    {
        odbObject.EnsureValid();

        var val = git_odb_object_type(odbObject.Ptr);

        return (Lg2ObjectType)val;
    }
}

public unsafe class Lg2Odb : SafeNativePointer<Lg2Odb, git_odb>, IReleaseNative<git_odb>
{
    internal Lg2Odb(git_odb* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_odb* pNative)
    {
        git_odb_free(pNative);
    }

    public static Lg2Odb Open(string objectsDir)
    {
        var u8ObjectsDir = new Lg2Utf8String(objectsDir);

        git_odb* pOdb = null;
        var rc = git_odb_open(&pOdb, u8ObjectsDir.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdb);
    }

    public static Lg2Odb New()
    {
        git_odb* pOdb = null;
        var rc = git_odb_new(&pOdb);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdb);
    }
}

public static unsafe class Lg2OdbExtenions
{
    public static void Refresh(this Lg2Odb odb)
    {
        odb.EnsureValid();

        var rc = git_odb_refresh(odb.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);
    }

    public static bool Exists(this Lg2Odb odb, ILg2WithOid oid)
    {
        odb.EnsureValid();

        var plainRef = oid.GetOidPlainRef();
        var val = git_odb_exists(odb.Ptr, plainRef.Ptr);

        return val != 0;
    }

    public static bool ExistsExt(this Lg2Odb odb, ILg2WithOid oid, Lg2OdbLookupFlags flags)
    {
        odb.EnsureValid();

        var plainRef = oid.GetOidPlainRef();
        var val = git_odb_exists_ext(odb.Ptr, plainRef.Ptr, (uint)flags);

        return val != 0;
    }

    public static Lg2OdbObject Read(this Lg2Odb odb, ref Lg2Oid oid)
    {
        odb.EnsureValid();

        git_odb_object* pOdbObject = null;

        fixed (git_oid* pOid = &oid._raw)
        {
            var rc = git_odb_read(&pOdbObject, odb.Ptr, pOid);
            Lg2Exception.RaiseIfNotOk(rc);
        }

        return new(pOdbObject);
    }

    public static Lg2OdbObject Read(this Lg2Odb odb, ILg2WithOid oid)
    {
        odb.EnsureValid();

        var oidRef = oid.GetOidPlainRef();
        git_odb_object* pOdbObject = null;
        var rc = git_odb_read(&pOdbObject, odb.Ptr, oidRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdbObject);
    }

    public static Lg2Oid Write(this Lg2Odb odb, Lg2RawData rawData, Lg2ObjectType objType)
    {
        odb.EnsureValid();

        Lg2Oid oid = new();
        var rc = git_odb_write(&oid._raw, odb.Ptr, rawData.Ptr, rawData.Len, (git_object_t)objType);
        Lg2Exception.RaiseIfNotOk(rc);

        return oid;
    }

    // public static Lg2Oid Write(this Lg2Odb odb, void* pData, nuint size, Lg2ObjectType objType)
    // {
    //     odb.EnsureValid();

    //     Lg2Oid oid = new();
    //     var rc = git_odb_write(&oid._raw, odb.Ptr, pData, size, (git_object_t)objType);
    //     Lg2Exception.RaiseIfNotOk(rc);

    //     return oid;
    // }
}

public unsafe class Lg2Commit
    : SafeNativePointer<Lg2Commit, git_commit>,
        IReleaseNative<git_commit>,
        ILg2WithOid
{
    internal Lg2Commit(git_commit* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_commit* pNative)
    {
        git_commit_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_commit_id(Ptr);

        return new(pOid);
    }
}

public static unsafe class Lg2CommitExtensions
{
    public static string GetSummary(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var summary = git_commit_summary(commit.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)summary) ?? string.Empty;

        return result;
    }

    public static Lg2Tree GetTree(this Lg2Commit commit)
    {
        commit.EnsureValid();

        git_tree* pTree = null;
        var rc = git_commit_tree(&pTree, commit.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pTree);
    }
}

public unsafe class Lg2Blob
    : SafeNativePointer<Lg2Blob, git_blob>,
        IReleaseNative<git_blob>,
        ILg2WithOid
{
    internal Lg2Blob(git_blob* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_blob* pNative)
    {
        git_blob_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();
        var pOid = git_blob_id(Ptr);

        return new(pOid);
    }

    public static bool DataIsBinary(sbyte* pData, nuint size)
    {
        if (pData is null || size == 0)
        {
            throw new ArgumentException($"Invalid input");
        }

        var val = git_blob_data_is_binary(pData, size);

        return val != 0;
    }
}

public static unsafe class Lg2BlobExtensions
{
    public static bool IsBinary(this Lg2Blob blob)
    {
        blob.EnsureValid();

        var val = git_blob_is_binary(blob.Ptr);

        return val != 0;
    }

    public static ulong GetRawSize(this Lg2Blob blob)
    {
        blob.EnsureValid();

        return git_blob_rawsize(blob.Ptr);
    }
}

public unsafe class Lg2Tag
    : SafeNativePointer<Lg2Tag, git_tag>,
        IReleaseNative<git_tag>,
        ILg2WithOid
{
    internal Lg2Tag(git_tag* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_tag* pNative)
    {
        git_tag_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();
        var pOid = git_tag_id(Ptr);

        return new(pOid);
    }
}

public unsafe class Lg2Object
    : SafeNativePointer<Lg2Object, git_object>,
        IReleaseNative<git_object>,
        ILg2WithOid
{
    internal Lg2Object(git_object* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_object* pNative)
    {
        git_object_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_object_id(Ptr);

        return new(pOid);
    }
}

public static unsafe class Lg2ObjectExtensions
{
    public static Lg2ObjectType GetType(this Lg2Object obj)
    {
        obj.EnsureValid();

        return (Lg2ObjectType)git_object_type(obj.Ptr);
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
