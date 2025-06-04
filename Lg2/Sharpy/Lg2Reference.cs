using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Reference
    : NativeSafePointer<Lg2Reference, git_reference>,
        INativeRelease<git_reference>
{
    public Lg2Reference()
        : this(default) { }

    internal Lg2Reference(git_reference* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_reference* pNative)
    {
        git_reference_free(pNative);
    }

    public static bool IsValidName(string refName)
    {
        using var u8RefName = new Lg2Utf8String(refName);
        int valid = default;
        var rc = git_reference_name_is_valid(&valid, u8RefName.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return valid != default;
    }

    public void SetTarget(Lg2OidPlainRef oidRef, string logMessage)
    {
        EnsureValid();

        using var u8LogMessage = new Lg2Utf8String(logMessage);

        git_reference* pRef = null;
        var rc = git_reference_set_target(&pRef, Ptr, oidRef.Ptr, u8LogMessage.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        ReleaseHandle();
        SetHandle((nint)pRef);
    }
}

public static unsafe class Lg2ReferenceExtensions
{
    public static Lg2RefType GetRefType(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var refType = git_reference_type(reference.Ptr);

        return (Lg2RefType)refType;
    }

    public static string GetName(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var pName = git_reference_name(reference.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pName) ?? string.Empty;

        return result;
    }

    public static Lg2OidPlainRef GetTarget(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var pOid = git_reference_target(reference.Ptr);
        if (pOid is null)
        {
            throw new InvalidOperationException("Reference's target is null");
        }

        return new(pOid);
    }

    public static string GetShorthand(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var pName = git_reference_shorthand(reference.Ptr);
        if (pName is null)
        {
            throw new InvalidOperationException("Reference's shorthand is null");
        }

        var result = Marshal.PtrToStringUTF8((nint)pName) ?? string.Empty;

        return result;
    }

    public static bool IsBranch(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var result = git_reference_is_branch(reference.Ptr);

        return result != 0;
    }

    public static bool IsRemote(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var result = git_reference_is_remote(reference.Ptr);

        return result != 0;
    }

    public static bool IsTag(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var result = git_reference_is_tag(reference.Ptr);

        return result != 0;
    }

    public static bool IsNote(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var result = git_reference_is_note(reference.Ptr);

        return result != 0;
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Reference NewReference(
        this Lg2Repository repo,
        string name,
        Lg2OidPlainRef oidRef,
        bool force,
        string logMessage
    )
    {
        repo.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);
        using var u8LogMessage = new Lg2Utf8String(logMessage);
        git_reference* pRef = null;
        var rc = git_reference_create(
            &pRef,
            repo.Ptr,
            u8Name,
            oidRef.Ptr,
            force ? 1 : 0,
            u8LogMessage.Ptr
        );
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pRef);
    }

    public static bool TryLookupRef(
        this Lg2Repository repo,
        string refName,
        out Lg2Reference reference
    )
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);
        git_reference* pRef = null;
        var rc = git_reference_lookup(&pRef, repo.Ptr, u8RefName.Ptr);

        if (rc != (int)GIT_OK)
        {
            reference = new Lg2Reference(default);
            return false;
        }
        else
        {
            reference = new Lg2Reference(pRef);
            return true;
        }
    }

    public static bool TrySearchRef(
        this Lg2Repository repo,
        string shorthand,
        out Lg2Reference reference
    )
    {
        repo.EnsureValid();

        using var u8Shorthand = new Lg2Utf8String(shorthand);
        git_reference* pRef = null;
        var rc = git_reference_dwim(&pRef, repo.Ptr, u8Shorthand.Ptr);

        if (rc != (int)GIT_OK)
        {
            reference = new Lg2Reference(default);
            return false;
        }
        else
        {
            reference = new Lg2Reference(pRef);
            return true;
        }
    }

    public static void GetRefOid(this Lg2Repository repo, string refName, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_reference_name_to_id(pOid, repo.Ptr, u8RefName.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }

    public static bool TryGetRefOid(this Lg2Repository repo, string refName, out Lg2Oid oid)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_reference_name_to_id(pOid, repo.Ptr, u8RefName.Ptr);
            if (rc == (int)GIT_ENOTFOUND)
            {
                return false;
            }
            Lg2Exception.ThrowIfNotOk(rc);
        }

        return true;
    }

    public static List<string> GetRefList(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_strarray refs = new();
        var rc = git_reference_list(&refs, repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        try
        {
            return refs.ToList();
        }
        finally
        {
            git_strarray_dispose(&refs);
        }
    }

    public static bool HasRefLog(this Lg2Repository repo, string refName)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        var result = git_reference_has_log(repo.Ptr, u8RefName.Ptr);

        return result != 0;
    }

    public static void EnsureRefLog(this Lg2Repository repo, string refName)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        var result = git_reference_ensure_log(repo.Ptr, u8RefName.Ptr);
        Lg2Exception.ThrowIfNotOk(result);
    }
}
