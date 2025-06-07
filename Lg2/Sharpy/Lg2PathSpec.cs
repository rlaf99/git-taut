using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2PathSpecMatchList
    : NativeSafePointer<Lg2PathSpecMatchList, git_pathspec_match_list>,
        INativeRelease<git_pathspec_match_list>
{
    public Lg2PathSpecMatchList()
        : this(default) { }

    internal Lg2PathSpecMatchList(git_pathspec_match_list* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_pathspec_match_list* pNative)
    {
        git_pathspec_match_list_free(pNative);
    }
}

public unsafe class Lg2PathSpec
    : NativeSafePointer<Lg2PathSpec, git_pathspec>,
        INativeRelease<git_pathspec>
{
    public Lg2PathSpec()
        : this(default) { }

    internal Lg2PathSpec(git_pathspec* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_pathspec* pNative)
    {
        git_pathspec_free(pNative);
    }

    public static Lg2PathSpec New(List<string> strings)
    {
        var strArray = Lg2StrArray.FromList(strings);
        return New(strArray);
    }

    public static Lg2PathSpec New(Lg2StrArray strArray)
    {
        var pPathSpec = strArray.Raw.NewPathSpec();

        return new(pPathSpec);
    }
}

internal static unsafe class Lg2PathSpecNativeExtensions { }

public static unsafe class Lg2PathSpecExtensions
{
    public static bool MatchPath(this Lg2PathSpec pathSpec, string path, Lg2PathSpecFlags flags)
    {
        pathSpec.EnsureValid();

        using var u8Path = new Lg2Utf8String(path);
        var result = git_pathspec_matches_path(pathSpec.Ptr, (uint)flags, u8Path.Ptr);

        return result != 0;
    }

    public static Lg2PathSpecMatchList GetTreeMatchList(
        Lg2PathSpec pathSpec,
        Lg2PathSpecFlags flags,
        Lg2Tree tree
    )
    {
        pathSpec.EnsureValid();
        tree.EnsureValid();

        git_pathspec_match_list* pMatchList = null;
        var rc = git_pathspec_match_tree(&pMatchList, tree.Ptr, (uint)flags, pathSpec.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pMatchList);
    }
}

unsafe partial class Lg2StrArrayNativeExtensions
{
    internal static git_pathspec* NewPathSpec(this git_strarray strarray)
    {
        git_pathspec* pPathSpec = null;

        var rc = git_pathspec_new(&pPathSpec, &strarray);
        Lg2Exception.ThrowIfNotOk(rc);

        return pPathSpec;
    }
}
