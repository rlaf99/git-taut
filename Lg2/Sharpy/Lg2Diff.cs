using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe ref struct Lg2DiffOptions
{
    internal git_diff_options Raw;

    public Lg2DiffOptions()
    {
        fixed (git_diff_options* ptr = &Raw)
        {
            git_diff_options_init(ptr, GIT_DIFF_OPTIONS_VERSION);
        }
    }

    public Lg2DiffOptionFlags Flags
    {
        get => (Lg2DiffOptionFlags)Raw.flags;
        set => Raw.flags = (uint)value;
    }
}

public unsafe ref struct Lg2DiffFindOptions
{
    internal git_diff_find_options Raw;

    public Lg2DiffFindFlags Flags
    {
        get => (Lg2DiffFindFlags)Raw.flags;
        set => Raw.flags = (uint)value;
    }

    public Lg2DiffFindOptions()
    {
        fixed (git_diff_find_options* ptr = &Raw)
        {
            var rc = git_diff_find_options_init(ptr, GIT_DIFF_FIND_OPTIONS_VERSION);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }
}

public unsafe class Lg2DiffFileOwnedRef<TOwner> : NativeOwnedRef<TOwner, git_diff_file>
    where TOwner : class
{
    internal Lg2DiffFileOwnedRef(TOwner owner, git_diff_file* pNative)
        : base(owner, pNative) { }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        return new(&Ptr->id);
    }

    public int GetOidValidLength()
    {
        EnsureValid();

        return Ptr->id_abbrev;
    }

    public string GetPath()
    {
        EnsureValid();

        var result = Marshal.PtrToStringUTF8((nint)Ptr->path);

        return result!;
    }

    public ulong GetSize()
    {
        EnsureValid();

        return Ptr->size;
    }

    public Lg2DiffFlags GetDiffFlags()
    {
        EnsureValid();

        return (Lg2DiffFlags)Ptr->flags;
    }

    public Lg2FileMode GetFileMode()
    {
        EnsureValid();

        return (Lg2FileMode)Ptr->mode;
    }

    public static implicit operator Lg2OidPlainRef(Lg2DiffFileOwnedRef<TOwner> ownedRef) =>
        ownedRef.GetOidPlainRef();
}

public unsafe class Lg2DiffDeltaOwnedRef<TOwner> : NativeOwnedRef<TOwner, git_diff_delta>
    where TOwner : class
{
    internal Lg2DiffDeltaOwnedRef(TOwner owner, git_diff_delta* pNative)
        : base(owner, pNative) { }

    public Lg2DeltaType GetStatus() => Ref.status.GetLg2();

    public Lg2DiffFlags GetFlags() => (Lg2DiffFlags)Ref.flags;

    public ushort GetSimilarity() => Ref.similarity;

    public ushort GetNumOfFiles() => Ref.nfiles;

    public Lg2DiffFileOwnedRef<TOwner> GetOldFile()
    {
        var owner = EnsureOwner();
        return new(owner, &Ptr->old_file);
    }

    public Lg2DiffFileOwnedRef<TOwner> GetNewFile()
    {
        var owner = EnsureOwner();
        return new(owner, &Ptr->new_file);
    }
}

public unsafe class Lg2Diff : NativeSafePointer<Lg2Diff, git_diff>, INativeRelease<git_diff>
{
    public Lg2Diff()
        : this(default) { }

    internal Lg2Diff(git_diff* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_diff* pNative)
    {
        git_diff_free(pNative);
    }
}

public static unsafe partial class Lg2DiffExtensions
{
    public static nuint GetDeltaCount(this Lg2Diff diff)
    {
        diff.EnsureValid();

        var result = git_diff_num_deltas(diff.Ptr);

        return result;
    }

    public static nuint GetDeltaCount(this Lg2Diff diff, Lg2DeltaType deltaType)
    {
        diff.EnsureValid();

        var result = git_diff_num_deltas_of_type(diff.Ptr, deltaType.GetRaw());

        return result;
    }

    public static Lg2DiffDeltaOwnedRef<Lg2Diff> GetDelta(this Lg2Diff diff, nuint idx)
    {
        diff.EnsureValid();

        var pDelta = git_diff_get_delta(diff.Ptr, idx);
        if (pDelta is null)
        {
            throw new InvalidOperationException($"Null pointer retrieved");
        }

        return new(diff, pDelta);
    }

    public static bool IsSortedCaseSensitively(this Lg2Diff diff)
    {
        diff.EnsureValid();

        var rc = git_diff_is_sorted_icase(diff.Ptr);

        return rc != 0;
    }

    public static Lg2DiffStats GetStats(this Lg2Diff diff)
    {
        diff.EnsureValid();

        git_diff_stats* pStats = null;
        var rc = git_diff_get_stats(&pStats, diff.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pStats);
    }

    public static void FindSimilar(this Lg2Diff diff)
    {
        diff.EnsureValid();

        var rc = git_diff_find_similar(diff.Ptr, null);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void FindSimilar(
        this Lg2Diff diff,
        scoped ref readonly Lg2DiffFindOptions options
    )
    {
        diff.EnsureValid();

        fixed (git_diff_find_options* pOptions = &options.Raw)
        {
            var rc = git_diff_find_similar(diff.Ptr, pOptions);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }

    public static void Merge(this Lg2Diff into, Lg2Diff from)
    {
        into.EnsureValid();
        from.EnsureValid();

        var rc = git_diff_merge(into.Ptr, from.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}

public unsafe class Lg2DiffStats
    : NativeSafePointer<Lg2DiffStats, git_diff_stats>,
        INativeRelease<git_diff_stats>
{
    public Lg2DiffStats()
        : this(default) { }

    internal Lg2DiffStats(git_diff_stats* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_diff_stats* pNative)
    {
        git_diff_stats_free(pNative);
    }
}

public static unsafe class Lg2DiffStatsExtensions
{
    public static nuint GetFilesChanged(this Lg2DiffStats diffStats)
    {
        diffStats.EnsureValid();

        var result = git_diff_stats_files_changed(diffStats.Ptr);

        return result;
    }

    public static nuint GetInsertions(this Lg2DiffStats diffStats)
    {
        diffStats.EnsureValid();

        var result = git_diff_stats_insertions(diffStats.Ptr);

        return result;
    }

    public static nuint GetDeletions(this Lg2DiffStats diffStats)
    {
        diffStats.EnsureValid();

        var result = git_diff_stats_deletions(diffStats.Ptr);

        return result;
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Diff NewDiff(this Lg2Repository repo, Lg2Tree srcTree, Lg2Tree dstTree)
    {
        repo.EnsureValid();
        srcTree.EnsureValid();
        dstTree.EnsureValid();

        git_diff* pDiff = null;
        var rc = git_diff_tree_to_tree(&pDiff, repo.Ptr, srcTree.Ptr, dstTree.Ptr, null);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pDiff);
    }

    public static Lg2Diff NewDiff(
        this Lg2Repository repo,
        Lg2Tree srcTree,
        Lg2Tree dstTree,
        scoped ref readonly Lg2DiffOptions options
    )
    {
        repo.EnsureValid();
        srcTree.EnsureValid();
        dstTree.EnsureValid();

        fixed (git_diff_options* pOptions = &options.Raw)
        {
            git_diff* pDiff = null;
            var rc = git_diff_tree_to_tree(&pDiff, repo.Ptr, srcTree.Ptr, dstTree.Ptr, pOptions);
            Lg2Exception.ThrowIfNotOk(rc);
            return new(pDiff);
        }
    }
}
