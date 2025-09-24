using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2StatusFlags GetFileStatus(this Lg2Repository repo, string path)
    {
        repo.EnsureValid();

        using var u8Path = new Lg2Utf8String(path);

        uint flags;
        var rc = git_status_file(&flags, repo.Ptr, u8Path.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return (Lg2StatusFlags)flags;
    }
}
