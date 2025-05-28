using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Tag
    : NativeSafePointer<Lg2Tag, git_tag>,
        INativeRelease<git_tag>,
        ILg2ObjectInfo
{
    public Lg2Tag()
        : this(default) { }

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

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Tag LookupTag(this Lg2Repository repo, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        git_tag* pTag = null;
        int rc;
        fixed (git_oid* pOid = &oid.Raw)
        {
            rc = git_tag_lookup(&pTag, repo.Ptr, pOid);
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Tag(pTag);
    }
}
