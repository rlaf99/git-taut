using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public interface ILg2ObjectInfo
{
    Lg2OidPlainRef GetOidPlainRef();
    Lg2ObjectType GetObjectType();
}

public static unsafe class Lg2objInfoExtensions
{
    public static string GetOidHexDigits(this ILg2ObjectInfo objInfo)
    {
        var oidRef = objInfo.GetOidPlainRef();
        return oidRef.Ref.Fmt();
    }

    public static string GetOidHexDigits(this ILg2ObjectInfo objInfo, int size)
    {
        var oidRef = objInfo.GetOidPlainRef();
        return oidRef.Ref.NFmt(size);
    }

    public static bool HasSameOid(this ILg2ObjectInfo objInfo, ILg2ObjectInfo other)
    {
        var objType = objInfo.GetObjectType();
        var otherObjType = other.GetObjectType();

        if (objType != otherObjType)
        {
            return false;
        }

        var oidRef = objInfo.GetOidPlainRef();
        var otherOidRef = other.GetOidPlainRef();

        return oidRef.Equals(otherOidRef);
    }
}

public unsafe class Lg2Object
    : NativeSafePointer<Lg2Object, git_object>,
        INativeRelease<git_object>,
        ILg2ObjectInfo
{
    public Lg2Object()
        : this(default) { }

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

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Object LookupObject(
        this Lg2Repository repo,
        ILg2ObjectInfo objInfo,
        Lg2ObjectType objType
    )
    {
        repo.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();

        git_object* pObj = null;
        var rc = git_object_lookup(&pObj, repo.Ptr, oidRef.Ptr, (git_object_t)objType);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pObj);
    }
}
