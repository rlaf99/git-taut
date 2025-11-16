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
    public static string GetOidHexDigits(this Lg2OidPlainRef plainRef)
    {
        return plainRef.Ref.Fmt();
    }

    public static string GetOidHexDigits(this Lg2OidPlainRef plainRef, int size)
    {
        return plainRef.Ref.NFmt(size);
    }

    public static ReadOnlySpan<byte> GetRawData(this Lg2OidPlainRef plainRef)
    {
        return MemoryMarshal.CreateReadOnlySpan(ref plainRef.Ref.id[0], GIT_OID_MAX_SIZE);
    }

    public static bool Equals(this Lg2OidPlainRef plainRef, Lg2OidPlainRef other)
    {
        if (plainRef.Ptr == other.Ptr)
        {
            return true;
        }

        var result = git_oid_cmp(plainRef.Ptr, other.Ptr);

        return result == 0;
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

    public bool Equals(scoped ref Lg2Oid other)
    {
        var ptr1 = (git_oid*)Unsafe.AsPointer(ref Raw);
        var ptr2 = (git_oid*)Unsafe.AsPointer(ref other.Raw);

        var result = git_oid_cmp(ptr1, ptr2);

        return result == 0;
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

    public void FromRaw(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* ptr = &MemoryMarshal.GetReference(bytes))
        {
            fixed (git_oid* pOid = &Raw)
            {
                var rc = git_oid_fromraw(pOid, ptr);
                Lg2Exception.ThrowIfNotOk(rc);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Lg2OidPlainRef(Lg2Oid oid) => oid.PlainRef;
}

public static unsafe class Lg2OidExtensions
{
    public static string GetOidHexDigits(this scoped ref Lg2Oid oid)
    {
        return oid.Raw.Fmt();
    }

    public static string GetOidHexDigits(this scoped ref Lg2Oid oid, int size)
    {
        return oid.Raw.NFmt(size);
    }

    public static Span<byte> GetRawData(this scoped ref Lg2Oid oid)
    {
        return MemoryMarshal.CreateSpan(ref oid.Raw.id[0], GIT_OID_MAX_SIZE);
    }

    public static bool PlainRefEquals(this scoped ref Lg2Oid oid, Lg2OidPlainRef otherRef)
    {
        return oid.PlainRef.Equals(otherRef);
    }
}

internal static unsafe class RawOidExtentions
{
    internal static string Fmt(this scoped ref git_oid oid)
    {
        const int NULL = 1;

        var pBuf = stackalloc sbyte[GIT_OID_MAX_HEXSIZE + NULL];

        fixed (git_oid* pOid = &oid)
        {
            var rc = git_oid_fmt(pBuf, pOid);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        var result = Marshal.PtrToStringUTF8((nint)pBuf);

        return result!;
    }

    internal static string NFmt(this scoped ref git_oid oid, int size)
    {
        if (size > GIT_OID_MAX_HEXSIZE)
        {
            throw new ArgumentOutOfRangeException(nameof(size), $"Value too large");
        }

        const int NULL = 1;

        var pBuf = stackalloc sbyte[size + NULL];

        fixed (git_oid* pOid = &oid)
        {
            var rc = git_oid_nfmt(pBuf, (nuint)size, pOid);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        var result = Marshal.PtrToStringUTF8((nint)pBuf);

        return result!;
    }

    internal static string PathFmt(this scoped ref git_oid oid)
    {
        const int SLASH_AND_NULL = 1 + 1;

        var pBuf = stackalloc sbyte[GIT_OID_MAX_HEXSIZE + SLASH_AND_NULL];

        fixed (git_oid* pOid = &oid)
        {
            var rc = git_oid_pathfmt(pBuf, pOid);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        var result = Marshal.PtrToStringUTF8((nint)pBuf);

        return result!;
    }
}
