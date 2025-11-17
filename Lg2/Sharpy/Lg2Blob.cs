using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public interface ILg2Blob : ILg2ObjectInfo
{
    // XXX: move more methods here
}

public unsafe class Lg2Blob
    : NativeSafePointer<Lg2Blob, git_blob>,
        INativeRelease<git_blob>,
        ILg2Blob
{
    public Lg2Blob()
        : this(default) { }

    internal Lg2Blob(git_blob* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_blob* pNative)
    {
        git_blob_free(pNative);
    }

    public static implicit operator Lg2OidPlainRef(Lg2Blob blob) => blob.GetOidPlainRef();

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();

        var pOid = git_blob_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType() => Lg2ObjectType.LG2_OBJECT_BLOB;

    public ReadStream NewReadStream()
    {
        EnsureValid();

        return new(this);
    }

    public class ReadStream(Lg2Blob sourceBlob) : Stream
    {
        long _totalRead;
        void* _ptr;
        byte* Data
        {
            get
            {
                if (_ptr is null)
                {
                    _ptr = git_blob_rawcontent(sourceBlob.Ptr);
                    if (_ptr is null)
                    {
                        throw new InvalidDataException($"blob content is null");
                    }
                }

                return (byte*)_ptr;
            }
        }

        readonly long _length = sourceBlob.GetObjectSize();

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => _totalRead;
            set => throw new NotSupportedException();
        }

        public override void Flush() { } // do nothing

        public override int Read(byte[] buffer, int offset, int count)
        {
            var target = buffer.AsSpan(offset, count);

            int dataRead = 0;
            while (_totalRead < _length && dataRead < target.Length)
            {
                target[dataRead++] = Data[_totalRead++];
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
    }
}

public static unsafe class Lg2BlobExtensions
{
    public static bool IsBinary(this Lg2Blob blob)
    {
        blob.EnsureValid();

        var val = git_blob_is_binary(blob.Ptr);

        return val != 0;
    }

    public static long GetObjectSize(this Lg2Blob blob)
    {
        blob.EnsureValid();

        return (long)git_blob_rawsize(blob.Ptr);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Blob LookupBlob(this Lg2Repository repo, Lg2OidPlainRef plainRef)
    {
        repo.EnsureValid();

        git_blob* pBlob = null;

        var rc = git_blob_lookup(&pBlob, repo.Ptr, plainRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pBlob);
    }
}
