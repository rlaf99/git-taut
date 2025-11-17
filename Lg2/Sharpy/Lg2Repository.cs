using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

unsafe partial class Lg2Methods
{
    public static bool Lg2TryDiscoverRepository(string path, out Lg2Repository repo)
    {
        using var u8Path = new Lg2Utf8String(path);
        git_buf buf = new();

        try
        {
            var rc = git_repository_discover(&buf, u8Path.Ptr, 0, null);
            if (rc == 0)
            {
                git_repository* ptr;
                rc = git_repository_open(&ptr, buf.ptr);
                Lg2Exception.ThrowIfNotOk(rc);

                repo = new(ptr);

                return true;
            }
            else
            {
                repo = new();

                return false;
            }
        }
        finally
        {
            git_buf_dispose(&buf);
        }
    }
}

public unsafe class Lg2Repository
    : NativeSafePointer<Lg2Repository, git_repository>,
        INativeRelease<git_repository>
{
    public Lg2Repository()
        : base(default) { }

    internal Lg2Repository(git_repository* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_repository* pNative)
    {
        git_repository_free(pNative);
    }

    public static Lg2Repository New(string repoPath)
    {
        using var u8Path = new Lg2Utf8String(repoPath);

        git_repository* ptr;
        var rc = git_repository_open(&ptr, u8Path.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(ptr);
    }

    public static Lg2Repository New(string repoPath, Lg2RepositoryOpenFlags flags)
    {
        using var u8Path = new Lg2Utf8String(repoPath);

        git_repository* ptr;
        var rc = git_repository_open_ext(&ptr, u8Path.Ptr, (uint)flags, null);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(ptr);
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

    public static string GetPath(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var pPath = git_repository_path(repo.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pPath) ?? string.Empty;

        return result;
    }

    public static Lg2Reference GetHead(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_reference* pRef = null;
        var rc = git_repository_head(&pRef, repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pRef);
    }

    public static Lg2Commit GetHeadCommit(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var headRef = repo.GetHead();
        var headOid = headRef.GetTarget();

        var result = repo.LookupCommit(headOid);

        return result;
    }

    public static void SetHead(this Lg2Repository repo, string refName)
    {
        repo.EnsureValid();

        using var u8Refname = new Lg2Utf8String(refName);

        var rc = git_repository_set_head(repo.Ptr, u8Refname.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static bool IsHeadUnborn(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var rc = git_repository_head_unborn(repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return rc == 1;
    }

    public static bool IsHeadDetached(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var rc = git_repository_head_detached(repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return rc == 1;
    }

    public static Lg2OidType GetOidType(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = git_repository_oid_type(repo.Ptr);

        return (Lg2OidType)result;
    }

    public static Lg2Index GetIndex(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_index* ptr;
        var rc = git_repository_index(&ptr, repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(ptr);
    }

    public static string GetWorkDirectory(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var ptr = git_repository_workdir(repo.Ptr);
        if (ptr is null)
        {
            throw new InvalidOperationException($"Result is null");
        }

        var result = Marshal.PtrToStringUTF8((nint)ptr)!;

        return result;
    }
}
