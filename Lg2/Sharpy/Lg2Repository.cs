using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Repository
    : NativeSafePointer<Lg2Repository, git_repository>,
        INativeRelease<git_repository>
{
    public Lg2Repository()
        : base(null) { }

    internal Lg2Repository(git_repository* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_repository* pNative)
    {
        git_repository_free(pNative);
    }

    static git_repository* OpenRaw(string repoPath)
    {
        using var u8Path = new Lg2Utf8String(repoPath);

        git_repository* pRepo;
        var rc = git_repository_open(&pRepo, u8Path.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return pRepo;
    }

    public void Open(string repoPath)
    {
        if (IsInvalid == false)
        {
            throw new InvalidOperationException($"{nameof(Lg2Repository)} is already opened");
        }

        var pRepo = OpenRaw(repoPath);

        SetHandle((nint)pRepo);
    }

    public static Lg2Repository New(string repoPath)
    {
        var u8Path = new Lg2Utf8String(repoPath);

        git_repository* pRepo;
        var rc = git_repository_open(&pRepo, u8Path.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Repository(pRepo);
    }
}

public static unsafe partial class Lg2RepositoryExtensions
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

    public static string GetPath(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var pPath = git_repository_path(repo.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pPath) ?? string.Empty;

        return result;
    }

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

    public static Lg2Blob LookupBlob(this Lg2Repository repo, ILg2ObjectInfo objInfo)
    {
        var oidPlainRef = objInfo.GetOidPlainRef();

        git_blob* pBlob = null;

        var rc = git_blob_lookup(&pBlob, repo.Ptr, oidPlainRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Blob(pBlob);
    }

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
