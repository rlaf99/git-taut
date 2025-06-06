using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public readonly unsafe ref struct Lg2OidPlainRef
{
    internal readonly git_oid* Ptr;

    internal ref git_oid Ref
    {
        get
        {
            EnsureValid();
            return ref (*Ptr);
        }
    }

    internal Lg2OidPlainRef(git_oid* pOid)
    {
        Ptr = pOid;
    }

    public void EnsureValid()
    {
        if (Ptr is null)
        {
            throw new InvalidOperationException($"Invalid {nameof(Lg2OidPlainRef)}");
        }
    }
}

public static unsafe class Lg2OidPlainRefExtensions
{
    public static ReadOnlySpan<byte> GetReadOnlyBytes(this Lg2OidPlainRef plainRef)
    {
        return MemoryMarshal.CreateReadOnlySpan(ref plainRef.Ref.id[0], GIT_OID_MAX_SIZE);
    }
}

public unsafe ref struct Lg2Oid
{
    internal git_oid Raw;

    public Lg2OidPlainRef PlainRef
    {
        get
        {
            var ptr = (git_oid*)Unsafe.AsPointer(ref Raw);

            return new(ptr);
        }
    }

    public void FromHexDigits(string hash)
    {
        var u8Hash = new Lg2Utf8String(hash);

        fixed (git_oid* pOid = &Raw)
        {
            var rc = git_oid_fromstr(pOid, u8Hash.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }

    public readonly string ToHexDigits()
    {
        return Raw.Fmt();
    }

    public readonly string ToHexDigits(int size)
    {
        return Raw.NFmt(size);
    }
}

public static unsafe class Lg2OidExtensions
{
    public static ReadOnlySpan<byte> GetReadOnlyBytes(this ref Lg2Oid oid)
    {
        return MemoryMarshal.CreateReadOnlySpan(ref oid.Raw.id[0], GIT_OID_MAX_SIZE);
    }

    public static Span<byte> GetBytes(this ref Lg2Oid oid)
    {
        return MemoryMarshal.CreateSpan(ref oid.Raw.id[0], GIT_OID_MAX_SIZE);
    }
}

internal static unsafe class Lg2OidNativeExtentions
{
    internal static string Fmt(ref readonly this git_oid oid)
    {
        var buf = stackalloc sbyte[GIT_OID_MAX_HEXSIZE + 1];
        fixed (git_oid* pOid = &oid)
        {
            var rc = git_oid_fmt(buf, pOid);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        var result = Marshal.PtrToStringUTF8((nint)buf) ?? string.Empty;

        return result;
    }

    internal static string NFmt(ref readonly this git_oid oid, int size)
    {
        if (size > GIT_OID_MAX_HEXSIZE)
        {
            throw new ArgumentOutOfRangeException(nameof(size), $"Value too large");
        }

        var buf = stackalloc sbyte[size + 1];

        fixed (git_oid* pOid = &oid)
        {
            var rc = git_oid_nfmt(buf, (nuint)size, pOid);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        var result = Marshal.PtrToStringUTF8((nint)buf) ?? string.Empty;

        return result;
    }
}

public interface ILg2ObjectInfo
{
    Lg2OidPlainRef GetOidPlainRef();
    Lg2ObjectType GetObjectType();
}

public static unsafe class Lg2objInfoExtensions
{
    public static string GetOidHexDigits(this ILg2ObjectInfo objInfo)
    {
        var oidRef = objInfo.GetOidPlainRef();
        return oidRef.Ref.Fmt();
    }
}

public unsafe class Lg2Object
    : NativeSafePointer<Lg2Object, git_object>,
        INativeRelease<git_object>,
        ILg2ObjectInfo
{
    public Lg2Object()
        : this(default) { }

    internal Lg2Object(git_object* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_object* pNative)
    {
        git_object_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_object_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        return (Lg2ObjectType)git_object_type(Ptr);
    }
}

public static unsafe class Lg2ObjectExtensions
{
    public static Lg2ObjectType GetObjectType(this Lg2Object obj)
    {
        obj.EnsureValid();

        return (Lg2ObjectType)git_object_type(obj.Ptr);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Object LookupObject(
        this Lg2Repository repo,
        ILg2ObjectInfo objInfo,
        Lg2ObjectType objType
    )
    {
        repo.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();

        git_object* pObj = null;
        var rc = git_object_lookup(&pObj, repo.Ptr, oidRef.Ptr, (git_object_t)objType);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pObj);
    }
}
