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
    public static void PushRef(this Lg2RevWalk revWalk, string refName)
    {
        revWalk.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);
        var rc = git_revwalk_push_ref(revWalk.Ptr, u8RefName.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);
    }

    public static void Push(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_revwalk_push(revWalk.Ptr, pOid);
            Lg2Exception.RaiseIfNotOk(rc);
        }
    }

    public static bool Next(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        revWalk.EnsureValid();

        var rc = (int)GIT_OK;
        fixed (git_oid* pOid = &oid.Raw)
        {
            rc = git_revwalk_next(pOid, revWalk.Ptr);
        }

        if (rc == (int)GIT_ITEROVER)
        {
            return false;
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return true;
    }

    internal static void Hide(this Lg2RevWalk revWalk, ref Lg2Oid oid)
    {
        revWalk.EnsureValid();

        var rc = (int)GIT_OK;
        fixed (git_oid* pOid = &oid.Raw)
        {
            rc = git_revwalk_hide(revWalk.Ptr, pOid);
        }
        Lg2Exception.RaiseIfNotOk(rc);
    }

    internal static void AddHideCallback(this Lg2RevWalk revWalk)
    {
        revWalk.EnsureValid();

        // TODO
        // git_revwalk_add_hide_cb();

        throw new NotImplementedException();
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2RevWalk NewRevWalk(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_revwalk* pRevWalk = null;
        var rc = git_revwalk_new(&pRevWalk, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2RevWalk(pRevWalk);
    }
}
