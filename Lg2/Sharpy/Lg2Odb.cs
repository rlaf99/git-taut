using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public readonly unsafe ref struct Lg2OidPlainRef
{
    internal readonly git_oid* Ptr;

    internal Lg2OidPlainRef(git_oid* pOid)
    {
        Ptr = pOid;
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

public unsafe class Lg2OdbObject
    : NativeSafePointer<Lg2OdbObject, git_odb_object>,
        INativeRelease<git_odb_object>,
        ILg2ObjectInfo
{
    public Lg2OdbObject()
        : this(default) { }

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
    public Lg2Odb()
        : this(default) { }

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

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Odb GetOdb(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_odb* pOdb = null;
        var rc = git_repository_odb(&pOdb, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdb);
    }
}
