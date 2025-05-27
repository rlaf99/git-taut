using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public interface ILg2TreeEntry : ILg2ObjectInfo
{
    string GetName();
}

public unsafe class Lg2TreeEntry
    : NativeSafePointer<Lg2TreeEntry, git_tree_entry>,
        INativeRelease<git_tree_entry>,
        ILg2TreeEntry
{
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

        return Lg2TreeEntryExtensions.GetName(Ptr);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return Lg2TreeEntryExtensions.GetType(Ptr);
    }
}

public readonly unsafe ref struct Lg2TreeEntryPlainRef : ILg2TreeEntry
{
    internal readonly git_tree_entry* Ptr;

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

public unsafe class Lg2TreeEntryOwnedRef : NativeOwnedRef<Lg2Tree, git_tree_entry>, ILg2TreeEntry
{
    internal Lg2TreeEntryOwnedRef(Lg2Tree owner, git_tree_entry* pNative)
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

        return Lg2TreeEntryExtensions.GetName(_pNative);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return Lg2TreeEntryExtensions.GetType(_pNative);
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

public unsafe class Lg2Tree
    : NativeSafePointer<Lg2Tree, git_tree>,
        INativeRelease<git_tree>,
        ILg2ObjectInfo
{
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
}

public static unsafe class Lg2TreeExtensions
{
    public static unsafe IEnumerator<Lg2TreeEntryOwnedRef> GetEnumerator(this Lg2Tree tree)
    {
        tree.EnsureValid();

        Lg2TreeEntryOwnedRef? ownedRef = null;

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

    public static unsafe Lg2TreeEntryPlainRef GetEntryPlainRefByIndex(this Lg2Tree tree, nuint idx)
    {
        tree.EnsureValid();

        var pEntry = git_tree_entry_byindex(tree.Ptr, idx);
        if (pEntry is null)
        {
            throw new ArgumentException($"Invalid {nameof(idx)}");
        }

        return new(pEntry);
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

        return new Lg2TreeEntryOwnedRef(tree, pEntry);
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

        return new Lg2TreeEntryOwnedRef(tree, pEntry);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Tree LookupTree(this Lg2Repository repo, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        git_tree* pTree = null;
        int rc;
        fixed (git_oid* pOid = &oid.Raw)
        {
            rc = git_tree_lookup(&pTree, repo.Ptr, pOid);
        }
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Tree(pTree);
    }

    public static Lg2Tree LookupTree(this Lg2Repository repo, ILg2ObjectInfo objInfo)
    {
        repo.EnsureValid();

        var oidPlainRef = objInfo.GetOidPlainRef();

        git_tree* pTree = null;
        var rc = git_tree_lookup(&pTree, repo.Ptr, oidPlainRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Tree(pTree);
    }
}
