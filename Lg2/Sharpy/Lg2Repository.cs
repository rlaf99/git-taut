using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Repository : SafeHandle
{
    public Lg2Repository()
        : base(default, true) { }

    public override bool IsInvalid => handle == default;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            git_repository_free((git_repository*)handle);
            handle = default;
        }

        return true;
    }

    Lg2Repository(git_repository* pRepo)
        : base(nint.Zero, true)
    {
        handle = (nint)pRepo;
    }

    public git_repository* Ptr => (git_repository*)handle;

    public static implicit operator git_repository*(Lg2Repository repo) =>
        (git_repository*)repo.handle;

    internal static Lg2Repository Open(string repoPath)
    {
        var u8Path = new Lg2Utf8String(repoPath);

        git_repository* pRepo;
        var rc = git_repository_open(&pRepo, u8Path.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Repository(pRepo);
    }
}

internal static unsafe class Lg2RepositoryExtensions
{
    static void EnsureValidRepository(Lg2Repository repo)
    {
        if (repo.IsInvalid)
        {
            throw new ArgumentException($"Invalid {nameof(repo)}");
        }
    }

    internal static bool IsBare(this Lg2Repository repo)
    {
        EnsureValidRepository(repo);

        var val = git_repository_is_bare(repo.Ptr);
        return val != 0;
    }

    internal static List<string> GetRefs(this Lg2Repository repo)
    {
        EnsureValidRepository(repo);

        var refs = new git_strarray();
        var rc = git_reference_list(&refs, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        using var lg2Refs = Lg2StrArray.FromNative(refs);

        return lg2Refs.ToList();
    }

    internal static Lg2Config GetConfig(this Lg2Repository repo)
    {
        EnsureValidRepository(repo);

        git_config* pConfig = null;
        var rc = git_repository_config(&pConfig, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Config(pConfig);
    }

    internal static Lg2RevWalk NewRevWalk(this Lg2Repository repo)
    {
        EnsureValidRepository(repo);

        git_revwalk* pRevWalk = null;
        var rc = git_revwalk_new(&pRevWalk, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2RevWalk(pRevWalk);
    }
}
