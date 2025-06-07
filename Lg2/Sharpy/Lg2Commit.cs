using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public static Lg2SignatureOwnedRef<Lg2Commit> GetAuthor(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var pSig = git_commit_author(commit.Ptr);
        if (pSig is null)
        {
            throw new InvalidOperationException($"Failed to get author from commit");
        }

        return new(commit, pSig);
    }

    public static Lg2SignatureOwnedRef<Lg2Commit> GetCommitter(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var pSig = git_commit_committer(commit.Ptr);
        if (pSig is null)
        {
            throw new InvalidOperationException($"Failed to get committer from commit");
        }

        return new(commit, pSig);
    }

    public static DateTimeOffset GetCommitTime(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var time = git_commit_time(commit.Ptr);
        var offset = git_commit_time_offset(commit.Ptr);

        return DateTimeOffset.FromUnixTimeSeconds(time).ToOffset(TimeSpan.FromMinutes(offset));
    }

    public static string GetMessage(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var pMessage = git_commit_message(commit.Ptr);
        if (pMessage is null)
        {
            throw new InvalidOperationException($"Failed to get message from commit");
        }

        var result = Marshal.PtrToStringUTF8((nint)pMessage);

        return result!;
    }

    public static string GetMessageEncoding(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var pEncoding = git_commit_message_encoding(commit.Ptr);
        if (pEncoding is null)
        {
            throw new InvalidOperationException($"Failed to get message encoding from commit");
        }

        var result = Marshal.PtrToStringUTF8((nint)pEncoding);

        return result!;
    }

    public static uint GetParentCount(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var result = git_commit_parentcount(commit.Ptr);

        return result;
    }

    public static List<Lg2Commit> GetParents(this Lg2Commit commit)
    {
        commit.EnsureValid();

        var count = git_commit_parentcount(commit.Ptr);

        var result = new List<Lg2Commit>((int)count);

        for (uint i = 0; i < count; i++)
        {
            git_commit* pParent = null;
            var rc = git_commit_parent(&pParent, commit.Ptr, i);
            Lg2Exception.ThrowIfNotOk(rc);

            result.Add(new(pParent));
        }

        return result;
    }

    public static Lg2CommitAmend NewAmend(this Lg2Commit commit)
    {
        return new(commit);
    }
}

public unsafe ref struct Lg2SignaturePlainRef
{
    internal readonly git_signature* Ptr;

    internal ref git_signature Ref
    {
        get
        {
            EnsureValid();
            return ref (*Ptr);
        }
    }

    internal Lg2SignaturePlainRef(git_signature* pSig)
    {
        Ptr = pSig;
    }

    public void EnsureValid()
    {
        if (Ptr is null)
        {
            throw new InvalidOperationException($"Invalid {nameof(Lg2SignaturePlainRef)}");
        }
    }
}

public unsafe class Lg2SignatureOwnedRef<TOwner> : NativeOwnedRef<TOwner, git_signature>
    where TOwner : class
{
    internal Lg2SignatureOwnedRef(TOwner owner, git_signature* pNative)
        : base(owner, pNative) { }

    public static implicit operator Lg2SignaturePlainRef(Lg2SignatureOwnedRef<TOwner> ownedRef) =>
        new(ownedRef.Ptr);
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

    public static void NewCommit(
        this Lg2Repository repo,
        Lg2SignaturePlainRef author,
        Lg2SignaturePlainRef committer,
        string message,
        Lg2Tree tree,
        List<Lg2Commit> parents,
        scoped ref Lg2Oid oid
    )
    {
        repo.EnsureValid();
        tree.EnsureValid();

        for (int i = 0; i < parents.Count; i++)
        {
            parents[i].EnsureValid();
        }

        var pCommits = stackalloc git_commit*[parents.Count];
        using var u8Message = new Lg2Utf8String(message);

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_commit_create(
                pOid,
                repo.Ptr,
                null,
                author.Ptr,
                committer.Ptr,
                null,
                u8Message.Ptr,
                tree.Ptr,
                (nuint)parents.Count,
                pCommits
            );
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }
}
