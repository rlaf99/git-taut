using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

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
    public static ReadOnlySpan<byte> GetObjectData(this Lg2OdbObject odbObject)
    {
        odbObject.EnsureValid();

        var ptr = git_odb_object_data(odbObject.Ptr);
        var len = git_odb_object_size(odbObject.Ptr);

        var result = new ReadOnlySpan<byte>(ptr, (int)len);

        return result;
    }

    public static long GetObjectSize(this Lg2OdbObject odbObject)
    {
        odbObject.EnsureValid();

        var result = git_odb_object_size(odbObject.Ptr);

        return (long)result;
    }

    public static Lg2OdbObjectReadStream NewReadStream(this Lg2OdbObject odbObject)
    {
        return new(odbObject);
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
    public static int Read(this Lg2OdbStream strm, Span<byte> data)
    {
        strm.EnsureValid();

        fixed (byte* ptr = &MemoryMarshal.GetReference(data))
        {
            var bytesRead = git_odb_stream_read(strm.Ptr, (sbyte*)ptr, (nuint)data.Length);
            if (bytesRead < 0)
            {
                Lg2Exception.ThrowIfNotOk(bytesRead);
            }

            return bytesRead;
        }
    }

    public static void Write(this Lg2OdbStream strm, ReadOnlySpan<byte> data)
    {
        strm.EnsureValid();

        fixed (byte* ptr = data)
        {
            var rc = git_odb_stream_write(strm.Ptr, (sbyte*)ptr, (nuint)data.Length);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }

    public static void FinalizeWrite(this Lg2OdbStream strm, scoped ref Lg2Oid oid)
    {
        strm.EnsureValid();

        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_odb_stream_finalize_write(pOid, strm.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
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

public unsafe class Lg2OdbReadStream : Stream, ILg2ObjectType
{
    readonly Lg2OdbStream _strm;
    readonly long _length;
    readonly Lg2ObjectType _objType;
    internal Lg2ObjectType ObjectType => _objType;

    long _totalRead;

    internal Lg2OdbReadStream(Lg2OdbStream rstrm, long length, Lg2ObjectType objType)
    {
        _strm = rstrm;
        _length = length;
        _objType = objType;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _totalRead;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dataRead = _strm.Read(buffer.AsSpan(offset, count));

        _totalRead += dataRead;

        return dataRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (disposing)
        {
            _strm.Dispose();
        }

        base.Dispose(disposing);
    }

    public Lg2ObjectType GetObjectType()
    {
        return _objType;
    }
}

public unsafe class Lg2OdbWriteStream : Stream
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
        set => throw new NotSupportedException();
    }

    public override void Flush() { } // do nonthing

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var data = buffer.AsSpan(offset, count);

        _wstrm.Write(data);
    }

    bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (disposing)
        {
            _wstrm.Dispose();
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
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pOdb);
    }

    public static Lg2Odb New()
    {
        git_odb* pOdb = null;
        var rc = git_odb_new(&pOdb);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pOdb);
    }

    public static void Hash(ReadOnlySpan<byte> data, Lg2ObjectType objType, scoped ref Lg2Oid oid)
    {
        var oidPtr = (git_oid*)Unsafe.AsPointer(ref oid.Raw);
        fixed (byte* dataPtr = data)
        {
            var rc = git_odb_hash(oidPtr, dataPtr, (nuint)data.Length, (git_object_t)objType);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }
}

public static unsafe class Lg2OdbExtenions
{
    public static void Refresh(this Lg2Odb odb)
    {
        odb.EnsureValid();

        var rc = git_odb_refresh(odb.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static bool Exists(this Lg2Odb odb, Lg2OidPlainRef oidRef)
    {
        odb.EnsureValid();

        var val = git_odb_exists(odb.Ptr, oidRef.Ptr);

        return val != 0;
    }

    public static bool ExistsExt(this Lg2Odb odb, Lg2OidPlainRef oidRef, Lg2OdbLookupFlags flags)
    {
        odb.EnsureValid();

        var val = git_odb_exists_ext(odb.Ptr, oidRef.Ptr, (uint)flags);

        return val != 0;
    }

    public static Lg2OdbObject Read(this Lg2Odb odb, Lg2OidPlainRef oidRef)
    {
        odb.EnsureValid();

        git_odb_object* pOdbObject = null;

        var rc = git_odb_read(&pOdbObject, odb.Ptr, oidRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pOdbObject);
    }

    public static Stream ReadAsStream(this Lg2Odb odb, Lg2OidPlainRef oidRef)
    {
        int rc;

        {
            git_odb_stream* pOdbStream = null;
            nuint len;
            git_object_t type;

            rc = git_odb_open_rstream(&pOdbStream, &len, &type, odb.Ptr, oidRef.Ptr);

            if (rc == 0)
            {
                Lg2OdbStream strm = new(pOdbStream);

                return new Lg2OdbReadStream(strm, (long)len, type.GetLg2());
            }
        }

        if (rc != (int)GIT_ENOTFOUND)
        {
            Lg2Exception.ThrowIfNotOk(rc);
        }

        var odbObject = odb.Read(oidRef);

        return odbObject.NewReadStream();
    }

    public static void Write(
        this Lg2Odb odb,
        ReadOnlySpan<byte> objData,
        Lg2ObjectType objType,
        scoped ref Lg2Oid oid
    )
    {
        odb.EnsureValid();

        fixed (byte* dataPtr = objData)
        {
            fixed (git_oid* pOid = &oid.Raw)
            {
                var rc = git_odb_write(
                    pOid,
                    odb.Ptr,
                    dataPtr,
                    (nuint)objData.Length,
                    (git_object_t)objType
                );
                Lg2Exception.ThrowIfNotOk(rc);
            }
        }
    }

    public static bool CopyObjectIfNotExists(
        this Lg2Odb odb,
        Lg2Odb otherOdb,
        Lg2OidPlainRef oidRef,
        bool refreshOtherOdb = false
    )
    {
        bool alreadyExists;

        if (refreshOtherOdb)
        {
            alreadyExists = otherOdb.Exists(oidRef);
        }
        else
        {
            alreadyExists = otherOdb.ExistsExt(oidRef, Lg2OdbLookupFlags.LG2_ODB_LOOKUP_NO_REFRESH);
        }

        if (alreadyExists)
        {
            return false;
        }

        using var readStream = odb.ReadAsStream(oidRef);

        if (readStream is not ILg2ObjectType objType)
        {
            throw new InvalidOperationException(
                $"{nameof(readStream)} does not implement {nameof(ILg2ObjectType)}"
            );
        }

        using var writeStream = otherOdb.OpenWriteStream(
            readStream.Length,
            objType.GetObjectType()
        );

        readStream.CopyTo(writeStream);

        Lg2Oid oid = new();
        writeStream.FinalizeWrite(ref oid);

        if (oidRef.Equals(oid.PlainRef) == false)
        {
            throw new InvalidDataException($"Copying object results in different oids");
        }

        return true;
    }

    public static Lg2OdbReadStream OpenReadStream(this Lg2Odb odb, Lg2OidPlainRef oidRef)
    {
        odb.EnsureValid();

        git_odb_stream* pOdbStream = null;
        nuint len;
        git_object_t type;
        var rc = git_odb_open_rstream(&pOdbStream, &len, &type, odb.Ptr, oidRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        Lg2OdbStream strm = new(pOdbStream);

        return new(strm, (long)len, type.GetLg2());
    }

    public static Lg2OdbWriteStream OpenWriteStream(
        this Lg2Odb odb,
        long objSize,
        Lg2ObjectType objType
    )
    {
        odb.EnsureValid();

        if (objSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(objSize), $"Invalid value '{objSize}'");
        }

        git_odb_stream* pOdbStream = null;
        var rc = git_odb_open_wstream(&pOdbStream, odb.Ptr, (ulong)objSize, (git_object_t)objType);
        Lg2Exception.ThrowIfNotOk(rc);

        Lg2OdbStream strm = new(pOdbStream);

        return new(strm);
    }
}

public unsafe class Lg2OdbObjectReadStream : Stream, ILg2ObjectType
{
    readonly Lg2OdbObject _odbObject;
    readonly byte* _ptr;
    readonly long _len;
    readonly Lg2ObjectType _type;
    long _totalRead;

    internal Lg2OdbObjectReadStream(Lg2OdbObject odbObject)
    {
        odbObject.EnsureValid();

        _odbObject = odbObject;

        _ptr = (byte*)git_odb_object_data(odbObject.Ptr);
        _len = (long)git_odb_object_size(odbObject.Ptr);
        _type = git_odb_object_type(odbObject.Ptr).GetLg2();
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _len;

    public override long Position
    {
        get => _totalRead;
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _len);
            _totalRead = value;
        }
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var dataRead = 0;

        var target = buffer.AsSpan(offset, count);

        while (_totalRead < _len && dataRead < target.Length)
        {
            target[dataRead++] = _ptr[_totalRead++];
        }

        return dataRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    bool _isDisposed;

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        if (disposing)
        {
            _odbObject.Dispose();
        }

        base.Dispose(disposing);
    }

    public Lg2ObjectType GetObjectType()
    {
        return _type;
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Odb GetOdb(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_odb* pOdb = null;
        var rc = git_repository_odb(&pOdb, repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pOdb);
    }
}
