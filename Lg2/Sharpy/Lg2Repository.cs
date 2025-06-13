using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

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

        git_repository* pRepo;
        var rc = git_repository_open(&pRepo, u8Path.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pRepo);
    }

    public static bool TryDiscover(string path, out Lg2Repository repo)
    {
        using var u8Path = new Lg2Utf8String(path);
        git_buf buf = new();

        try
        {
            var rc = git_repository_discover(&buf, u8Path.Ptr, 0, null);
            if (rc == 0)
            {
                git_repository* pRepo;
                rc = git_repository_open(&pRepo, u8Path.Ptr);
                Lg2Exception.ThrowIfNotOk(rc);

                repo = new(pRepo);

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

    public static void SetHead(this Lg2Repository repo, string refName)
    {
        repo.EnsureValid();

        using var u8Refname = new Lg2Utf8String(refName);

        var rc = git_repository_set_head(repo.Ptr, u8Refname.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}
