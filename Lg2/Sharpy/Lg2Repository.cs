using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Repository
    : SafeNativePointer<Lg2Repository, git_repository>,
        IReleaseNative<git_repository>
{
    internal Lg2Repository(git_repository* pNative)
        : base(pNative) { }

    public static void ReleaseNative(git_repository* pNative)
    {
        git_repository_free(pNative);
    }

    public static implicit operator git_repository*(Lg2Repository repo) =>
        (git_repository*)repo.handle;

    public static Lg2Repository Open(string repoPath)
    {
        var u8Path = new Lg2Utf8String(repoPath);

        git_repository* pRepo;
        var rc = git_repository_open(&pRepo, u8Path.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Repository(pRepo);
    }
}

public static unsafe class Lg2RepositoryExtensions
{
    public static bool IsBare(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var val = git_repository_is_bare(repo.Ptr);
        return val != 0;
    }

    public static List<string> GetRefList(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var refs = new git_strarray();
        var rc = git_reference_list(&refs, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        using var lg2Refs = Lg2StrArray.FromNative(refs);

        return lg2Refs.ToList();
    }

    public static Lg2Config GetConfig(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_config* pConfig = null;
        var rc = git_repository_config(&pConfig, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Config(pConfig);
    }

    public static Lg2Odb GetOdb(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_odb* pOdb = null;
        var rc = git_repository_odb(&pOdb, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdb);
    }

    public static Lg2RevWalk NewRevWalk(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_revwalk* pRevWalk = null;
        var rc = git_revwalk_new(&pRevWalk, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2RevWalk(pRevWalk);
    }

    public static Lg2Object LookupObject(
        this Lg2Repository repo,
        ILg2WithOid oid,
        Lg2ObjectType objType
    )
    {
        repo.EnsureValid();

        var oidRef = oid.GetOidPlainRef();

        git_object* pObj = null;
        var rc = git_object_lookup(&pObj, repo.Ptr, oidRef.Ptr, (git_object_t)objType);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pObj);
    }

    public static Lg2Tree LookupTree(this Lg2Repository repo, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        git_tree* pTree = null;
        var rc = (int)GIT_OK;
        fixed (git_oid* pOid = &oid._raw)
        {
            rc = git_tree_lookup(&pTree, repo.Ptr, pOid);
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Tree(pTree);
    }

    public static Lg2Tree LookupTree(this Lg2Repository repo, ILg2WithOid oid)
    {
        repo.EnsureValid();

        var oidPlainRef = oid.GetOidPlainRef();

        git_tree* pTree = null;
        var rc = (int)GIT_OK;
        rc = git_tree_lookup(&pTree, repo.Ptr, oidPlainRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Tree(pTree);
    }

    public static Lg2Blob LookupBlob(this Lg2Repository repo, ILg2WithOid oid)
    {
        var oidPlainRef = oid.GetOidPlainRef();

        git_blob* pBlob = null;
        var rc = (int)GIT_OK;
        rc = git_blob_lookup(&pBlob, repo.Ptr, oidPlainRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Blob(pBlob);
    }

    public static Lg2Commit LookupCommit(this Lg2Repository repo, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        git_commit* pCommit = null;
        var rc = (int)GIT_OK;
        fixed (git_oid* git_oid = &oid._raw)
        {
            rc = git_commit_lookup(&pCommit, repo.Ptr, git_oid);
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Commit(pCommit);
    }

    public static Lg2Tag LookupTag(this Lg2Repository repo, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        git_tag* pTag = null;
        var rc = (int)GIT_OK;
        fixed (git_oid* git_oid = &oid._raw)
        {
            rc = git_tag_lookup(&pTag, repo.Ptr, git_oid);
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Tag(pTag);
    }
}
