using System.Runtime.CompilerServices;
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

public unsafe class Lg2IndexEntryOwnedRef<TOwner> : NativeOwnedRef<TOwner, git_index_entry>
    where TOwner : class
{
    internal Lg2IndexEntryOwnedRef(TOwner owner, git_index_entry* pNative)
        : base(owner, pNative) { }

    public static implicit operator Lg2OidPlainRef(Lg2IndexEntryOwnedRef<TOwner> ownedRef) =>
        ownedRef.GetOidPlainRef();
}

public static unsafe class Lg2IndexEntryOwnedRefExtensions
{
    public static Lg2OidPlainRef GetOidPlainRef<TOwner>(this Lg2IndexEntryOwnedRef<TOwner> entry)
        where TOwner : class
    {
        entry.EnsureValid();

        return new(&entry.Ptr->id);
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

    public static Lg2IndexEntryOwnedRef<Lg2Index> GetEntry(
        this Lg2Index index,
        string path,
        int stage
    )
    {
        index.EnsureValid();

        using var u8Path = new Lg2Utf8String(path);

        var ptr = git_index_get_bypath(index.Ptr, u8Path.Ptr, stage);
        if (ptr is null)
        {
            throw new InvalidOperationException($"Null value returned");
        }

        return new(index, ptr);
    }
}
