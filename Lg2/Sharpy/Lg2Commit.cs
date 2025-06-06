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

    public static implicit operator Lg2OidPlainRef(Lg2Commit commit) => commit.GetOidPlainRef();
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
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pTree);
    }

    public static Lg2CommitAmend NewAmend(this Lg2Commit commit)
    {
        return new(commit);
    }
}

public unsafe class Lg2CommitAmend
{
    Lg2Commit _commit;

    internal Lg2CommitAmend(Lg2Commit commit)
    {
        _commit = commit;
    }

    public string? Message { get; set; }
    public Lg2Tree? Tree { get; set; }

    public void Write(ref Lg2Oid oid)
    {
        if (Message is null && Tree is null)
        {
            throw new InvalidOperationException("Nothing to amend");
        }

        using var u8Message = Message is null ? null : new Lg2Utf8String(Message);
        var pMessage = u8Message is null ? null : u8Message.Ptr;
        var pTree = Tree is null ? null : Tree.Ptr;

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_commit_amend(pOid, _commit.Ptr, null, null, null, null, pMessage, pTree);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Commit LookupCommit(this Lg2Repository repo, Lg2OidPlainRef oidRef)
    {
        repo.EnsureValid();

        git_commit* pCommit = null;
        var rc = git_commit_lookup(&pCommit, repo.Ptr, oidRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pCommit);
    }
}
