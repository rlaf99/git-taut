using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Index : NativeSafePointer<Lg2Index, git_index>, INativeRelease<git_index>
{
    public Lg2Index()
        : this(default) { }

    internal Lg2Index(git_index* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_index* pNative)
    {
        git_index_free(pNative);
    }

    public static Lg2Index New()
    {
        git_index* pIndex = null;
        var rc = git_index_new(&pIndex);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pIndex);
    }
}

public static unsafe class Lg2IndexExtensions
{
    public static void ReadTree(this Lg2Index index, Lg2Tree tree)
    {
        index.EnsureValid();
        tree.EnsureValid();

        var rc = git_index_read_tree(index.Ptr, tree.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}
