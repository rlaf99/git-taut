using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Commit
    : NativeSafePointer<Lg2Commit, git_commit>,
        INativeRelease<git_commit>,
        ILg2ObjectInfo
{
    public Lg2Commit()
        : this(default) { }

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

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Commit LookupCommit(this Lg2Repository repo, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        git_commit* pCommit = null;
        int rc;
        fixed (git_oid* pOid = &oid.Raw)
        {
            rc = git_commit_lookup(&pCommit, repo.Ptr, pOid);
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Commit(pCommit);
    }
}
