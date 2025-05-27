using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Reference
    : NativeSafePointer<Lg2Reference, git_reference>,
        INativeRelease<git_reference>
{
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
        Lg2Exception.RaiseIfNotOk(rc);

        return valid != default;
    }

    public void SetTarget(Lg2OidPlainRef oidRef, string logMessage)
    {
        EnsureValid();

        using var u8LogMessage = new Lg2Utf8String(logMessage);

        git_reference* pRef = null;
        var rc = git_reference_set_target(&pRef, Ptr, oidRef.Ptr, u8LogMessage.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

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
        Lg2Exception.RaiseIfNotOk(rc);

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

    public static void GetOidForRef(this Lg2Repository repo, string refName, ref Lg2Oid oid)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);
        int rc;
        fixed (git_oid* pOid = &oid.Raw)
        {
            rc = git_reference_name_to_id(pOid, repo.Ptr, u8RefName.Ptr);
        }
        Lg2Exception.RaiseIfNotOk(rc);
    }
}
