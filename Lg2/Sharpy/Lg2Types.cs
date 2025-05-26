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

public sealed unsafe class Lg2Config
    : NativeSafePointer<Lg2Config, git_config>,
        INativeRelease<git_config>
{
    internal Lg2Config(git_config* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_config* pNative)
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
    : NativeSafePointer<Lg2RevWalk, git_revwalk>,
        INativeRelease<git_revwalk>
{
    internal Lg2RevWalk(git_revwalk* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_revwalk* pNative)
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

    public static void Push(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_revwalk_push(revWalk.Ptr, pOid);
            Lg2Exception.RaiseIfNotOk(rc);
        }
    }

    public static bool Next(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        revWalk.EnsureValid();

        var rc = (int)GIT_OK;
        fixed (git_oid* pOid = &oid.Raw)
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
        fixed (git_oid* pOid = &oid.Raw)
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

public interface ILg2ObjectInfo
{
    Lg2OidPlainRef GetOidPlainRef();
    Lg2ObjectType GetObjectType();
}

public static unsafe class Lg2objInfoExtensions
{
    public static string GetOidString(this ILg2ObjectInfo objInfo)
    {
        var oidRef = objInfo.GetOidPlainRef();
        return Lg2Oid.ToString(oidRef.Ptr);
    }
}

public unsafe ref struct Lg2Oid
{
    internal git_oid Raw;

    public void FromString(string hash)
    {
        var u8Hash = new Lg2Utf8String(hash);

        fixed (git_oid* pOid = &Raw)
        {
            var rc = git_oid_fromstr(pOid, u8Hash.Ptr);
            Lg2Exception.RaiseIfNotOk(rc);
        }
    }

    public override string ToString()
    {
        fixed (git_oid* pOid = &Raw)
        {
            return ToString(pOid);
        }
    }

    public string ToString(int size)
    {
        fixed (git_oid* pOid = &Raw)
        {
            return ToString(pOid, size);
        }
    }

    public static string ToString(git_oid* pOid)
    {
        var buf = stackalloc sbyte[GIT_OID_MAX_HEXSIZE + 1];
        var rc = git_oid_fmt(buf, pOid);
        Lg2Exception.RaiseIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)buf) ?? string.Empty;

        return result;
    }

    public static string ToString(git_oid* pOid, int size)
    {
        if (size > GIT_OID_MAX_HEXSIZE)
        {
            throw new ArgumentException($"Value too large", nameof(size));
        }

        var buf = stackalloc sbyte[size + 1];

        var rc = git_oid_nfmt(buf, (nuint)size, pOid);
        Lg2Exception.RaiseIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)buf) ?? string.Empty;

        return result;
    }
}

public static class Lg2OidExtensions { }

public unsafe ref struct Lg2RawData
{
    internal void* Ptr;
    internal nuint Len;
}

public unsafe class Lg2OdbObject
    : NativeSafePointer<Lg2OdbObject, git_odb_object>,
        INativeRelease<git_odb_object>,
        ILg2ObjectInfo
{
    internal Lg2OdbObject(git_odb_object* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_odb_object* pNative)
    {
        git_odb_object_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_odb_object_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        var val = git_odb_object_type(Ptr);

        return (Lg2ObjectType)val;
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
}

public unsafe class Lg2Odb : NativeSafePointer<Lg2Odb, git_odb>, INativeRelease<git_odb>
{
    internal Lg2Odb(git_odb* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_odb* pNative)
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

    public static bool Exists(this Lg2Odb odb, ILg2ObjectInfo objInfo)
    {
        odb.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();
        var val = git_odb_exists(odb.Ptr, oidRef.Ptr);

        return val != 0;
    }

    public static bool ExistsExt(this Lg2Odb odb, ILg2ObjectInfo objInfo, Lg2OdbLookupFlags flags)
    {
        odb.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();
        var val = git_odb_exists_ext(odb.Ptr, oidRef.Ptr, (uint)flags);

        return val != 0;
    }

    public static Lg2OdbObject Read(this Lg2Odb odb, ref Lg2Oid oid)
    {
        odb.EnsureValid();

        git_odb_object* pOdbObject = null;

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_odb_read(&pOdbObject, odb.Ptr, pOid);
            Lg2Exception.RaiseIfNotOk(rc);
        }

        return new(pOdbObject);
    }

    public static Lg2OdbObject Read(this Lg2Odb odb, ILg2ObjectInfo objInfo)
    {
        odb.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();
        git_odb_object* pOdbObject = null;
        var rc = git_odb_read(&pOdbObject, odb.Ptr, oidRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdbObject);
    }

    public static Lg2Oid Write(this Lg2Odb odb, Lg2RawData rawData, Lg2ObjectType objType)
    {
        odb.EnsureValid();

        Lg2Oid oid = new();
        var rc = git_odb_write(&oid.Raw, odb.Ptr, rawData.Ptr, rawData.Len, (git_object_t)objType);
        Lg2Exception.RaiseIfNotOk(rc);

        return oid;
    }

    public static void CopyObjectToAnother(
        this Lg2Odb thisOdb,
        Lg2Odb thatOdb,
        ref Lg2Oid oid,
        out Lg2ObjectType objType
    )
    {
        var odbObject = thisOdb.Read(ref oid);
        objType = odbObject.GetObjectType();
        thatOdb.Write(odbObject.GetRawData(), objType);
    }

    public static bool TryCopyObjectToAnother(
        this Lg2Odb thisOdb,
        Lg2Odb thatOdb,
        ILg2ObjectInfo objInfo,
        bool refreshThatOdb = false
    )
    {
        bool alreadyExists;

        if (refreshThatOdb)
        {
            alreadyExists = thatOdb.Exists(objInfo);
        }
        else
        {
            alreadyExists = thatOdb.ExistsExt(objInfo, Lg2OdbLookupFlags.LG2_ODB_LOOKUP_NO_REFRESH);
        }

        if (alreadyExists)
        {
            return false;
        }

        var odbObject = thisOdb.Read(objInfo);
        thatOdb.Write(odbObject.GetRawData(), odbObject.GetObjectType());

        return true;
    }
}

public unsafe class Lg2Commit
    : NativeSafePointer<Lg2Commit, git_commit>,
        INativeRelease<git_commit>,
        ILg2ObjectInfo
{
    internal Lg2Commit(git_commit* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_commit* pNative)
    {
        git_commit_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_commit_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        return Lg2ObjectType.LG2_OBJECT_COMMIT;
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
    : NativeSafePointer<Lg2Blob, git_blob>,
        INativeRelease<git_blob>,
        ILg2ObjectInfo
{
    internal Lg2Blob(git_blob* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_blob* pNative)
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

    public Lg2ObjectType GetObjectType()
    {
        return Lg2ObjectType.LG2_OBJECT_BLOB;
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
    : NativeSafePointer<Lg2Tag, git_tag>,
        INativeRelease<git_tag>,
        ILg2ObjectInfo
{
    internal Lg2Tag(git_tag* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_tag* pNative)
    {
        git_tag_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();
        var pOid = git_tag_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        return Lg2ObjectType.LG2_OBJECT_TAG;
    }
}

public unsafe class Lg2Object
    : NativeSafePointer<Lg2Object, git_object>,
        INativeRelease<git_object>,
        ILg2ObjectInfo
{
    internal Lg2Object(git_object* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_object* pNative)
    {
        git_object_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_object_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return (Lg2ObjectType)git_object_type(Ptr);
    }
}

public static unsafe class Lg2ObjectExtensions
{
    public static Lg2ObjectType GetObjectType(this Lg2Object obj)
    {
        obj.EnsureValid();

        return (Lg2ObjectType)git_object_type(obj.Ptr);
    }
}

public unsafe class Lg2Reference
    : NativeSafePointer<Lg2Reference, git_reference>,
        INativeRelease<git_reference>
{
    internal Lg2Reference(git_reference* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_reference* pNative)
    {
        git_reference_free(pNative);
    }
}

public static unsafe class Lg2ReferenceExtensions
{
    internal static Lg2RefType GetRefType(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var refType = git_reference_type(reference.Ptr);

        return (Lg2RefType)refType;
    }

    internal static string GetName(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var pName = git_reference_name(reference.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pName) ?? string.Empty;

        return result;
    }
}

public unsafe class Lg2RefSpec
    : NativeSafePointer<Lg2RefSpec, git_refspec>,
        INativeRelease<git_refspec>
{
    internal Lg2RefSpec(git_refspec* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_refspec* pNative)
    {
        git_refspec_free(pNative);
    }

    static bool TryParse(string input, bool isFetch, ref Lg2RefSpec refSpec)
    {
        using var u8Input = new Lg2Utf8String(input);

        git_refspec* pRefSpec = null;
        var rc = git_refspec_parse(&pRefSpec, u8Input.Ptr, isFetch ? 1 : 0);
        if (rc != (int)GIT_OK)
        {
            return false;
        }

        refSpec.SetHandle((nint)pRefSpec);

        return true;
    }

    public static bool TryParseForPush(string input, out Lg2RefSpec refSpec)
    {
        refSpec = new(null);

        return TryParse(input, false, ref refSpec);
    }

    public static bool TryParseForFetch(string input, out Lg2RefSpec refSpec)
    {
        refSpec = new(null);

        return TryParse(input, true, ref refSpec);
    }
}

public static unsafe class Lg2RefSpecExtensions
{
    public static string GetSrc(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var pSrc = git_refspec_src(refSpec.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pSrc) ?? string.Empty;

        return result;
    }

    public static string GetDst(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var pDst = git_refspec_dst(refSpec.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pDst) ?? string.Empty;

        return result;
    }

    public static string GetString(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var pStr = git_refspec_string(refSpec.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pStr) ?? string.Empty;

        return result;
    }

    static bool GetForce(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var force = git_refspec_force(refSpec.Ptr);

        return force != 0;
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
