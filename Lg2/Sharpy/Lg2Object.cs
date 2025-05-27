using System.Runtime.InteropServices;
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
    public static string GetOidString(this ILg2ObjectInfo objInfo)
    {
        var oidRef = objInfo.GetOidPlainRef();
        return Lg2Oid.ToString(oidRef.Ptr);
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
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pObj);
    }
}
