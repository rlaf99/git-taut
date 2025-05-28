using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Blob
    : NativeSafePointer<Lg2Blob, git_blob>,
        INativeRelease<git_blob>,
        ILg2ObjectInfo
{
    public Lg2Blob()
        : this(default) { }

    internal Lg2Blob(git_blob* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_blob* pNative)
    {
        git_blob_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();
        var pOid = git_blob_id(Ptr);

        return new(pOid);
    }

    public static bool DataIsBinary(sbyte* pData, nuint size)
    {
        if (pData is null || size == 0)
        {
            throw new ArgumentException($"Invalid input");
        }

        var val = git_blob_data_is_binary(pData, size);

        return val != 0;
    }

    public Lg2ObjectType GetObjectType()
    {
        return Lg2ObjectType.LG2_OBJECT_BLOB;
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

    public static ulong GetRawSize(this Lg2Blob blob)
    {
        blob.EnsureValid();

        return git_blob_rawsize(blob.Ptr);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Blob LookupBlob(this Lg2Repository repo, ILg2ObjectInfo objInfo)
    {
        var oidPlainRef = objInfo.GetOidPlainRef();

        git_blob* pBlob = null;

        var rc = git_blob_lookup(&pBlob, repo.Ptr, oidPlainRef.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Blob(pBlob);
    }
}
