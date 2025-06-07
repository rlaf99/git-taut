using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2RevWalk
    : NativeSafePointer<Lg2RevWalk, git_revwalk>,
        INativeRelease<git_revwalk>
{
    public Lg2RevWalk()
        : this(default) { }

    internal Lg2RevWalk(git_revwalk* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_revwalk* pNative)
    {
        git_revwalk_free(pNative);
    }
}

public static unsafe class Lg2RevWalkExtensions
{
    public static bool Next(this Lg2RevWalk revWalk, scoped ref Lg2Oid oid)
    {
        revWalk.EnsureValid();

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_revwalk_next(pOid, revWalk.Ptr);
            if (rc == (int)GIT_ITEROVER)
            {
                return false;
            }
            Lg2Exception.ThrowIfNotOk(rc);
        }

        return true;
    }

    public static void Push(this Lg2RevWalk revWalk, string refName)
    {
        revWalk.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);
        var rc = git_revwalk_push_ref(revWalk.Ptr, u8RefName.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void Push(this Lg2RevWalk revWalk, Lg2OidPlainRef plainRef)
    {
        revWalk.EnsureValid();

        var rc = git_revwalk_push(revWalk.Ptr, plainRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void PushHead(this Lg2RevWalk revWalk)
    {
        revWalk.EnsureValid();

        var rc = git_revwalk_push_head(revWalk.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void PushGlob(this Lg2RevWalk revWalk, string glob)
    {
        revWalk.EnsureValid();

        using var u8Glob = new Lg2Utf8String(glob);
        var rc = git_revwalk_push_glob(revWalk.Ptr, u8Glob.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void Hide(this Lg2RevWalk revWalk, string refName)
    {
        revWalk.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);
        var rc = git_revwalk_hide_ref(revWalk.Ptr, u8RefName.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void Hide(this Lg2RevWalk revWalk, Lg2OidPlainRef plainRef)
    {
        revWalk.EnsureValid();

        var rc = git_revwalk_hide(revWalk.Ptr, plainRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void HideHead(this Lg2RevWalk revWalk)
    {
        revWalk.EnsureValid();

        var rc = git_revwalk_hide_head(revWalk.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void HideGlob(this Lg2RevWalk revWalk, string glob)
    {
        revWalk.EnsureValid();

        using var u8Glob = new Lg2Utf8String(glob);
        var rc = git_revwalk_hide_glob(revWalk.Ptr, u8Glob.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void ResetSorting(this Lg2RevWalk revWalk, Lg2SortFlags sortMode)
    {
        revWalk.EnsureValid();

        var rc = git_revwalk_sorting(revWalk.Ptr, (uint)sortMode);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2RevWalk NewRevWalk(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_revwalk* pRevWalk = null;
        var rc = git_revwalk_new(&pRevWalk, repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new Lg2RevWalk(pRevWalk);
    }
}
