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
    public static ReadOnlySpan<byte> GetBytes(this Lg2OidPlainRef plainRef)
    {
        return MemoryMarshal.CreateReadOnlySpan(ref plainRef.Ref.id[0], GIT_OID_MAX_SIZE);
    }
}

public unsafe ref struct Lg2Oid
{
    internal git_oid Raw;

    public ReadOnlySpan<byte> GetBytes()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Raw.id[0], GIT_OID_MAX_SIZE);
    }

    public void FromString(string hash)
    {
        var u8Hash = new Lg2Utf8String(hash);

        fixed (git_oid* pOid = &Raw)
        {
            var rc = git_oid_fromstr(pOid, u8Hash.Ptr);
            Lg2Exception.RaiseIfNotOk(rc);
        }
    }

    public override readonly string ToString()
    {
        return Raw.Fmt();
    }

    public readonly string ToString(int size)
    {
        return Raw.NFmt(size);
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
            Lg2Exception.RaiseIfNotOk(rc);
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
            Lg2Exception.RaiseIfNotOk(rc);
        }

        var result = Marshal.PtrToStringUTF8((nint)buf) ?? string.Empty;

        return result;
    }
}

public unsafe class Lg2OdbObject
    : NativeSafePointer<Lg2OdbObject, git_odb_object>,
        INativeRelease<git_odb_object>,
        ILg2ObjectInfo
{
    public Lg2OdbObject()
        : this(default) { }

    internal Lg2OdbObject(git_odb_object* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_odb_object* pNative)
    {
        git_odb_object_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_odb_object_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        EnsureValid();

        var val = git_odb_object_type(Ptr);

        return (Lg2ObjectType)val;
    }
}

public static unsafe class Lg2OdbObjectExtensions
{
    public static Lg2RawData GetRawData(this Lg2OdbObject odbObject)
    {
        odbObject.EnsureValid();

        Lg2RawData rawData = new()
        {
            Ptr = (nint)git_odb_object_data(odbObject.Ptr),
            Len = (long)git_odb_object_size(odbObject.Ptr),
        };

        return rawData;
    }

    public static long GetRawSize(this Lg2OdbObject odbObject)
    {
        odbObject.EnsureValid();

        var result = git_odb_object_size(odbObject.Ptr);

        return (long)result;
    }
}

public unsafe class Lg2OdbStream
    : NativeSafePointer<Lg2OdbStream, git_odb_stream>,
        INativeRelease<git_odb_stream>
{
    public Lg2OdbStream()
        : this(default) { }

    internal Lg2OdbStream(git_odb_stream* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_odb_stream* pNative)
    {
        git_odb_stream_free(pNative);
    }
}

public static unsafe class Lg2OdbStreamExtensions
{
    public static void Read(this Lg2OdbStream rstrm, ref Lg2RawData rawData)
    {
        rstrm.EnsureValid();

        var rc = git_odb_stream_read(rstrm.Ptr, (sbyte*)rawData.Ptr, (nuint)rawData.Len);
        Lg2Exception.RaiseIfNotOk(rc);
    }

    public static void Write(this Lg2OdbStream wstrm, scoped ref readonly Lg2RawData rawData)
    {
        wstrm.EnsureValid();

        var rc = git_odb_stream_write(wstrm.Ptr, (sbyte*)rawData.Ptr, (nuint)rawData.Len);
        Lg2Exception.RaiseIfNotOk(rc);
    }

    public static void FinalizeWrite(this Lg2OdbStream wstrm, ref Lg2Oid oid)
    {
        wstrm.EnsureValid();

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_odb_stream_finalize_write(pOid, wstrm.Ptr);
            Lg2Exception.RaiseIfNotOk(rc);
        }
    }

    public static long GetDeclaredBytes(this Lg2OdbStream strm)
    {
        strm.EnsureValid();

        return (long)strm.Ptr->declared_size;
    }

    public static long GetReceivedBytes(this Lg2OdbStream strm)
    {
        strm.EnsureValid();

        return (long)strm.Ptr->received_bytes;
    }
}

public class Lg2OdbReadStream : Stream
{
    readonly Lg2OdbStream _rstrm;

    internal Lg2OdbReadStream(Lg2OdbStream rstrm)
    {
        _rstrm = rstrm;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _rstrm.GetDeclaredBytes();

    public override long Position
    {
        get => _rstrm.GetReceivedBytes();
        set => throw new NotSupportedException($"Setting {nameof(Position)} is not supported");
    }

    public override void Flush() { } // do nothing

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException($"'{nameof(Seek)}' not supported");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException($"'{nameof(SetLength)}' not supported");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException($"'{nameof(Write)}' not supported");
    }

    bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed == false)
        {
            _disposed = true;

            if (disposing)
            {
                _rstrm.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}

public class Lg2OdbWriteStream : Stream
{
    readonly Lg2OdbStream _wstrm;

    internal Lg2OdbWriteStream(Lg2OdbStream wstrm)
    {
        _wstrm = wstrm;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _wstrm.GetDeclaredBytes();

    public override long Position
    {
        get => _wstrm.GetReceivedBytes();
        set => throw new NotSupportedException($"Setting {nameof(Position)} is not supported");
    }

    public override void Flush() { } // do nonthing

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException($"'{nameof(Read)}' not supported");
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException($"'{nameof(Seek)}' not supported");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException($"'{nameof(SetLength)}' not supported");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed == false)
        {
            _disposed = true;

            if (disposing)
            {
                _wstrm.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    public void FinalizeWrite(ref Lg2Oid oid)
    {
        _wstrm.FinalizeWrite(ref oid);
        _wstrm.Close();
    }
}

public unsafe class Lg2Odb : NativeSafePointer<Lg2Odb, git_odb>, INativeRelease<git_odb>
{
    public Lg2Odb()
        : this(default) { }

    internal Lg2Odb(git_odb* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_odb* pNative)
    {
        git_odb_free(pNative);
    }

    public static Lg2Odb Open(string objectsDir)
    {
        var u8ObjectsDir = new Lg2Utf8String(objectsDir);

        git_odb* pOdb = null;
        var rc = git_odb_open(&pOdb, u8ObjectsDir.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdb);
    }

    public static Lg2Odb New()
    {
        git_odb* pOdb = null;
        var rc = git_odb_new(&pOdb);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdb);
    }

    public static void Hash(Lg2RawData rawData, Lg2ObjectType objType, ref Lg2Oid oid)
    {
        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_odb_hash(
                pOid,
                (void*)rawData.Ptr,
                (nuint)rawData.Len,
                (git_object_t)objType
            );
            Lg2Exception.RaiseIfNotOk(rc);
        }
    }
}

public static unsafe class Lg2OdbExtenions
{
    public static void Refresh(this Lg2Odb odb)
    {
        odb.EnsureValid();

        var rc = git_odb_refresh(odb.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);
    }

    public static bool Exists(this Lg2Odb odb, ILg2ObjectInfo objInfo)
    {
        odb.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();
        var val = git_odb_exists(odb.Ptr, oidRef.Ptr);

        return val != 0;
    }

    public static bool ExistsExt(this Lg2Odb odb, ILg2ObjectInfo objInfo, Lg2OdbLookupFlags flags)
    {
        odb.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();
        var val = git_odb_exists_ext(odb.Ptr, oidRef.Ptr, (uint)flags);

        return val != 0;
    }

    public static Lg2OdbObject Read(this Lg2Odb odb, ref Lg2Oid oid)
    {
        odb.EnsureValid();

        git_odb_object* pOdbObject = null;

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_odb_read(&pOdbObject, odb.Ptr, pOid);
            Lg2Exception.RaiseIfNotOk(rc);
        }

        return new(pOdbObject);
    }

    public static Lg2OdbObject Read(this Lg2Odb odb, ILg2ObjectInfo objInfo)
    {
        odb.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();
        git_odb_object* pOdbObject = null;
        var rc = git_odb_read(&pOdbObject, odb.Ptr, oidRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdbObject);
    }

    public static Lg2Oid Write(this Lg2Odb odb, Lg2RawData rawData, Lg2ObjectType objType)
    {
        odb.EnsureValid();

        Lg2Oid oid = new();
        var rc = git_odb_write(
            &oid.Raw,
            odb.Ptr,
            (void*)rawData.Ptr,
            (nuint)rawData.Len,
            (git_object_t)objType
        );
        Lg2Exception.RaiseIfNotOk(rc);

        return oid;
    }

    public static void CopyObjectToAnother(
        this Lg2Odb thisOdb,
        Lg2Odb thatOdb,
        ref Lg2Oid oid,
        out Lg2ObjectType objType
    )
    {
        var odbObject = thisOdb.Read(ref oid);
        objType = odbObject.GetObjectType();
        thatOdb.Write(odbObject.GetRawData(), objType);
    }

    public static bool TryCopyObjectToAnother(
        this Lg2Odb thisOdb,
        Lg2Odb thatOdb,
        ILg2ObjectInfo objInfo,
        bool refreshThatOdb = false
    )
    {
        bool alreadyExists;

        if (refreshThatOdb)
        {
            alreadyExists = thatOdb.Exists(objInfo);
        }
        else
        {
            alreadyExists = thatOdb.ExistsExt(objInfo, Lg2OdbLookupFlags.LG2_ODB_LOOKUP_NO_REFRESH);
        }

        if (alreadyExists)
        {
            return false;
        }

        var odbObject = thisOdb.Read(objInfo);
        thatOdb.Write(odbObject.GetRawData(), odbObject.GetObjectType());

        return true;
    }

    internal static Lg2OdbReadStream OpenReadStream(this Lg2Odb odb, ILg2ObjectInfo objInfo)
    {
        odb.EnsureValid();

        var oidRef = objInfo.GetOidPlainRef();
        git_odb_stream* pOdbStream = null;
        nuint len;
        git_object_t type;
        var rc = git_odb_open_rstream(&pOdbStream, &len, &type, odb.Ptr, oidRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        Lg2OdbStream strm = new(pOdbStream);

        return new(strm);
    }

    internal static Lg2OdbWriteStream OpenWriteStream(
        this Lg2Odb odb,
        ulong objSize,
        Lg2ObjectType objType
    )
    {
        odb.EnsureValid();

        git_odb_stream* pOdbStream = null;
        var rc = git_odb_open_wstream(&pOdbStream, odb.Ptr, objSize, (git_object_t)objType);
        Lg2Exception.RaiseIfNotOk(rc);

        Lg2OdbStream strm = new(pOdbStream);

        return new(strm);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Odb GetOdb(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_odb* pOdb = null;
        var rc = git_repository_odb(&pOdb, repo.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new(pOdb);
    }
}
