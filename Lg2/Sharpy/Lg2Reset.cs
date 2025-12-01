using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

unsafe partial class Lg2RepositoryExtensions
{
    public static void Reset(this Lg2Repository repo, Lg2Object target, Lg2ResetType resetType)
    {
        repo.EnsureValid();
        target.EnsureValid();

        var rc = git_reset(repo.Ptr, target.Ptr, (git_reset_t)resetType, null);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}
