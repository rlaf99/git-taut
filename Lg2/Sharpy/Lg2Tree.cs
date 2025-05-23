using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public interface ILg2TreeEntry : ILg2WithOid
{
    string GetName();
    Lg2ObjectType GetObjectType();
}

public unsafe class Lg2TreeEntry
    : SafeNativePointer<Lg2TreeEntry, git_tree_entry>,
        IReleaseNative<git_tree_entry>,
        ILg2TreeEntry
{
    internal Lg2TreeEntry(git_tree_entry* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_tree_entry* pNative)
    {
        git_tree_entry_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_tree_entry_id(Ptr);

        return new Lg2OidPlainRef(pOid);
    }

    public string GetName()
    {
        EnsureValid();

        return Lg2TreeEntryExtensions.GetName(Ptr);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return Lg2TreeEntryExtensions.GetType(Ptr);
    }
}

public unsafe class Lg2TreeEntryRef : ILg2TreeEntry
{
    readonly WeakReference<Lg2Tree> _treeWeakRef;
    readonly git_tree_entry* _pEntry;

    internal Lg2TreeEntryRef(Lg2Tree tree, git_tree_entry* pEntry)
    {
        _treeWeakRef = new WeakReference<Lg2Tree>(tree);
        _pEntry = pEntry;
    }

    void EnsureValid()
    {
        if (_treeWeakRef.TryGetTarget(out _) == false)
        {
            throw new InvalidOperationException(
                $"The instance of {nameof(git_tree_entry)} is not valid"
            );
        }
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_tree_entry_id(_pEntry);

        return new Lg2OidPlainRef(pOid);
    }

    public string GetName()
    {
        EnsureValid();

        return Lg2TreeEntryExtensions.GetName(_pEntry);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return Lg2TreeEntryExtensions.GetType(_pEntry);
    }
}

public static unsafe class Lg2TreeEntryExtensions
{
    internal static string GetName(git_tree_entry* pEntry)
    {
        var pName = git_tree_entry_name(pEntry);
        return Marshal.PtrToStringUTF8((nint)pName) ?? string.Empty;
    }

    internal static Lg2ObjectType GetType(git_tree_entry* pEntry)
    {
        return (Lg2ObjectType)git_tree_entry_type(pEntry);
    }
}

public unsafe class Lg2Tree : SafeNativePointer<Lg2Tree, git_tree>, IReleaseNative<git_tree>
{
    internal Lg2Tree(git_tree* pNative)
        : base(pNative) { }

    public static unsafe void ReleaseNative(git_tree* pNative)
    {
        git_tree_free(pNative);
    }
}

public static unsafe class Lg2TreeExtensions
{
    public static void EnsureValidIndex(this Lg2Tree tree, nuint index)
    {
        var count = git_tree_entrycount(tree.Ptr);
        if (index >= count)
        {
            throw new ArgumentException($"Invalid {nameof(index)}");
        }
    }

    public static nuint GetEntryCount(this Lg2Tree tree)
    {
        tree.EnsureValid();

        return git_tree_entrycount(tree.Ptr);
    }

    public static ILg2TreeEntry GetEntryByIndex(this Lg2Tree tree, nuint index)
    {
        tree.EnsureValid();

        var pEntry = git_tree_entry_byindex(tree.Ptr, index);
        if (pEntry is null)
        {
            throw new ArgumentException($"Invalid {nameof(index)}");
        }

        return new Lg2TreeEntryRef(tree, pEntry);
    }

    public static ILg2TreeEntry GetEntryByName(this Lg2Tree tree, string name)
    {
        tree.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        var pEntry = git_tree_entry_byname(tree.Ptr, u8Name.Ptr);
        if (pEntry is null)
        {
            throw new ArgumentException($"Invalid {nameof(name)}");
        }

        return new Lg2TreeEntryRef(tree, pEntry);
    }
}
