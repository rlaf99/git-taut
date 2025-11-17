using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public interface ILg2TreeEntry : ILg2ObjectInfo
{
    string GetName();

    Lg2TreeEntryPlainRef GetTreeEntryPlainRef();

    public Lg2FileMode GetFileMode();

    public Lg2FileMode GetFileModeRaw();
}

public unsafe class Lg2TreeEntry
    : NativeSafePointer<Lg2TreeEntry, git_tree_entry>,
        INativeRelease<git_tree_entry>,
        ILg2TreeEntry
{
    public Lg2TreeEntry()
        : this(default) { }

    internal Lg2TreeEntry(git_tree_entry* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_tree_entry* pNative)
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

        return GetName(Ptr);
    }

    public Lg2TreeEntryPlainRef GetTreeEntryPlainRef()
    {
        EnsureValid();

        return new(Ptr);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return GetObjectType(Ptr);
    }

    public Lg2FileMode GetFileMode()
    {
        EnsureValid();

        return GetFileMode(Ptr);
    }

    public Lg2FileMode GetFileModeRaw()
    {
        EnsureValid();

        return GetFileModeRaw(Ptr);
    }

    internal static string GetName(git_tree_entry* pEntry)
    {
        var pName = git_tree_entry_name(pEntry);
        return Marshal.PtrToStringUTF8((nint)pName) ?? string.Empty;
    }

    internal static Lg2ObjectType GetObjectType(git_tree_entry* pEntry)
    {
        return (Lg2ObjectType)git_tree_entry_type(pEntry);
    }

    internal static Lg2FileMode GetFileMode(git_tree_entry* pEntry)
    {
        return (Lg2FileMode)git_tree_entry_filemode(pEntry);
    }

    internal static Lg2FileMode GetFileModeRaw(git_tree_entry* pEntry)
    {
        return (Lg2FileMode)git_tree_entry_filemode_raw(pEntry);
    }

    public static implicit operator Lg2OidPlainRef(Lg2TreeEntry entry) => entry.GetOidPlainRef();
}

public readonly unsafe ref struct Lg2TreeEntryPlainRef
{
    internal readonly git_tree_entry* Ptr;

    internal ref git_tree_entry Ref
    {
        get
        {
            EnsureValid();
            return ref (*Ptr);
        }
    }

    internal Lg2TreeEntryPlainRef(git_tree_entry* pEntry)
    {
        Ptr = pEntry;
    }

    void EnsureValid()
    {
        if (Ptr == default)
        {
            throw new InvalidOperationException($"Invalid {nameof(Lg2TreeEntryPlainRef)}");
        }
    }
}

public unsafe class Lg2TreeEntryOwnedRef<TOwner>
    : NativeOwnedRef<TOwner, git_tree_entry>,
        ILg2TreeEntry
    where TOwner : class
{
    internal Lg2TreeEntryOwnedRef(TOwner owner, git_tree_entry* pNative)
        : base(owner, pNative) { }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_tree_entry_id(_pNative);

        return new Lg2OidPlainRef(pOid);
    }

    public string GetName()
    {
        EnsureValid();

        return Lg2TreeEntry.GetName(_pNative);
    }

    public Lg2TreeEntryPlainRef GetTreeEntryPlainRef()
    {
        EnsureValid();

        return new(Ptr);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return Lg2TreeEntry.GetObjectType(_pNative);
    }

    public Lg2FileMode GetFileMode()
    {
        return Lg2TreeEntry.GetFileMode(_pNative);
    }

    public Lg2FileMode GetFileModeRaw()
    {
        return Lg2TreeEntry.GetFileModeRaw(_pNative);
    }

    public static implicit operator Lg2OidPlainRef(Lg2TreeEntryOwnedRef<TOwner> ownedRef) =>
        ownedRef.GetOidPlainRef();
}

public interface ILg2Tree : ILg2ObjectInfo
{
    // XXX: move methods here
}

public unsafe class Lg2Tree
    : NativeSafePointer<Lg2Tree, git_tree>,
        INativeRelease<git_tree>,
        ILg2Tree
{
    public Lg2Tree()
        : this(default) { }

    internal Lg2Tree(git_tree* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_tree* pNative)
    {
        git_tree_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_tree_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        return Lg2ObjectType.LG2_OBJECT_TREE;
    }

    public static implicit operator Lg2OidPlainRef(Lg2Tree tree) => tree.GetOidPlainRef();
}

public static unsafe class Lg2TreeExtensions
{
    public static unsafe IEnumerator<Lg2TreeEntryOwnedRef<Lg2Tree>> GetEnumerator(this Lg2Tree tree)
    {
        tree.EnsureValid();

        Lg2TreeEntryOwnedRef<Lg2Tree>? ownedRef;

        for (nuint idx = 0; idx < tree.GetEntryCount(); idx++)
        {
            unsafe
            {
                var pEntry = git_tree_entry_byindex(tree.Ptr, idx);
                if (pEntry is null)
                {
                    throw new ArgumentException($"Invalid {nameof(idx)}");
                }

                ownedRef = new(tree, pEntry);
            }

            yield return ownedRef;
        }
    }

    public static nuint GetEntryCount(this Lg2Tree tree)
    {
        tree.EnsureValid();

        return git_tree_entrycount(tree.Ptr);
    }

    public static Lg2TreeEntryOwnedRef<Lg2Tree> GetEntry(this Lg2Tree tree, nuint index)
    {
        tree.EnsureValid();

        var pEntry = git_tree_entry_byindex(tree.Ptr, index);
        if (pEntry is null)
        {
            throw new ArgumentException($"Invalid {nameof(index)}");
        }

        return new(tree, pEntry);
    }

    public static Lg2TreeEntryOwnedRef<Lg2Tree> GetEntry(this Lg2Tree tree, string name)
    {
        tree.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        var pEntry = git_tree_entry_byname(tree.Ptr, u8Name.Ptr);
        if (pEntry is null)
        {
            throw new ArgumentException($"Invalid {nameof(name)}");
        }

        return new(tree, pEntry);
    }
}

public unsafe class Lg2TreeBuilder
    : NativeSafePointer<Lg2TreeBuilder, git_treebuilder>,
        INativeRelease<git_treebuilder>
{
    public Lg2TreeBuilder()
        : this(default) { }

    internal Lg2TreeBuilder(git_treebuilder* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_treebuilder* pNative)
    {
        git_treebuilder_free(pNative);
    }
}

public static unsafe class Lg2TreeBuilderExtensions
{
    public static void Clear(this Lg2TreeBuilder treeBuilder)
    {
        treeBuilder.EnsureValid();

        var rc = git_treebuilder_clear(treeBuilder.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static nuint GetEntryCount(this Lg2TreeBuilder treeBuilder)
    {
        treeBuilder.EnsureValid();

        var result = git_treebuilder_entrycount(treeBuilder.Ptr);

        return result;
    }

    public static ILg2TreeEntry GetEntry(this Lg2TreeBuilder treeBuilder, string name)
    {
        treeBuilder.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        var pEntry = git_treebuilder_get(treeBuilder.Ptr, u8Name.Ptr);
        if (pEntry is null)
        {
            throw new ArgumentException($"Invalid {nameof(name)}");
        }

        return new Lg2TreeEntryOwnedRef<Lg2TreeBuilder>(treeBuilder, pEntry);
    }

    public static void Insert(
        this Lg2TreeBuilder treeBuilder,
        string name,
        Lg2OidPlainRef oidRef,
        Lg2FileMode fileMode
    )
    {
        treeBuilder.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        var rc = git_treebuilder_insert(
            null,
            treeBuilder.Ptr,
            u8Name.Ptr,
            oidRef.Ptr,
            (git_filemode_t)fileMode
        );
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void Remove(this Lg2TreeBuilder treeBuilder, string name)
    {
        treeBuilder.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        var rc = git_treebuilder_remove(treeBuilder.Ptr, u8Name.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void Write(this Lg2TreeBuilder treeBuilder, ref Lg2Oid oid)
    {
        treeBuilder.EnsureValid();

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_treebuilder_write(pOid, treeBuilder.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Tree LookupTree(this Lg2Repository repo, Lg2OidPlainRef oidRef)
    {
        repo.EnsureValid();

        git_tree* pTree = null;
        var rc = git_tree_lookup(&pTree, repo.Ptr, oidRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pTree);
    }

    public static Lg2TreeBuilder NewTreeBuilder(this Lg2Repository repo, Lg2Tree? tree = null)
    {
        repo.EnsureValid();
        tree?.EnsureValid();

        git_treebuilder* pTreeBuilder = null;
        var rc = git_treebuilder_new(&pTreeBuilder, repo.Ptr, tree is null ? default : tree.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pTreeBuilder);
    }

    public static Lg2Object GetTreeEntryObject(this Lg2Repository repo, ILg2TreeEntry treeEntry)
    {
        repo.EnsureValid();

        var plainRef = treeEntry.GetTreeEntryPlainRef();

        git_object* pObj = null;
        var rc = git_tree_entry_to_object(&pObj, repo.Ptr, plainRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pObj);
    }
}
