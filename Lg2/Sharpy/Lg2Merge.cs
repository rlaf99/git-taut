using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

unsafe partial class Lg2RepositoryExtensions
{
    public static bool TryGetMergeBase(
        this Lg2Repository repo,
        Lg2OidPlainRef one,
        Lg2OidPlainRef two,
        scoped ref Lg2Oid oid
    )
    {
        repo.EnsureValid();

        fixed (git_oid* ptr = &oid.Raw)
        {
            var rc = git_merge_base(ptr, repo.Ptr, one.Ptr, two.Ptr);

            if (rc == (int)GIT_ENOTFOUND)
            {
                return false;
            }

            Lg2Exception.ThrowIfNotOk(rc);

            return true;
        }
    }
}
