using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

unsafe partial class Lg2Methods
{
    public static bool Lg2TagNameIsValid(string name)
    {
        var u8Name = new Lg2Utf8String(name);

        int valid = 0;
        var rc = git_tag_name_is_valid(&valid, u8Name.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return valid == 0;
    }
}

public interface ILg2Tag : ILg2ObjectInfo
{
    string GetName();

    Lg2SignaturePlainRef GetTagger();

    string GetMessage();

    Lg2Object GetTarget();

    Lg2ObjectType GetTargetType();
}

public unsafe class Lg2Tag : NativeSafePointer<Lg2Tag, git_tag>, INativeRelease<git_tag>, ILg2Tag
{
    public Lg2Tag()
        : this(default) { }

    internal Lg2Tag(git_tag* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_tag* pNative)
    {
        git_tag_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef() => Ref.GetOidPlainRef();

    public Lg2ObjectType GetObjectType() => Lg2ObjectType.LG2_OBJECT_TAG;

    public string GetName() => Ref.GetName();

    public Lg2SignaturePlainRef GetTagger() => Ref.GetTagger();

    public string GetMessage() => Ref.GetMessage();

    public Lg2Object GetTarget() => Ref.GetTarget();

    public Lg2ObjectType GetTargetType() => Ref.GetTargetType();
}

public unsafe class Lg2TagOwnedRef<TOwner> : NativeOwnedRef<TOwner, git_tag>, ILg2Tag
    where TOwner : class
{
    internal Lg2TagOwnedRef(TOwner owner, git_tag* pNative)
        : base(owner, pNative) { }

    public Lg2ObjectType GetObjectType() => Lg2ObjectType.LG2_OBJECT_TAG;

    public Lg2OidPlainRef GetOidPlainRef() => Ref.GetOidPlainRef();

    public string GetName() => Ref.GetName();

    public Lg2SignaturePlainRef GetTagger() => Ref.GetTagger();

    public string GetMessage() => Ref.GetMessage();

    public Lg2Object GetTarget() => Ref.GetTarget();

    public Lg2ObjectType GetTargetType() => Ref.GetTargetType();
}

static unsafe class RawTagExtensions
{
    internal static Lg2OidPlainRef GetOidPlainRef(this scoped ref git_tag tag)
    {
        fixed (git_tag* pTag = &tag)
        {
            var pOid = git_tag_id(pTag);
            if (pOid == null)
            {
                throw new InvalidOperationException($"Result is null");
            }

            return new(pOid);
        }
    }

    internal static Lg2ObjectType GetTargetType(this scoped ref git_tag tag)
    {
        fixed (git_tag* pTag = &tag)
        {
            var type = git_tag_target_type(pTag);
            return (Lg2ObjectType)type;
        }
    }

    internal static Lg2Object GetTarget(this scoped ref git_tag tag)
    {
        git_object* pObj = null;

        fixed (git_tag* pTag = &tag)
        {
            var rc = git_tag_target(&pObj, pTag);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        return new(pObj);
    }

    internal static string GetName(this scoped ref git_tag tag)
    {
        fixed (git_tag* pTag = &tag)
        {
            var pName = git_tag_name(pTag);
            if (pName == null)
            {
                throw new InvalidOperationException($"Result is null");
            }

            var result = Marshal.PtrToStringUTF8((nint)pName)!;

            return result;
        }
    }

    internal static Lg2SignaturePlainRef GetTagger(this scoped ref git_tag tag)
    {
        fixed (git_tag* pTag = &tag)
        {
            var pSig = git_tag_tagger(pTag);
            if (pSig is null)
            {
                throw new InvalidOperationException($"Result is null");
            }

            return new(pSig);
        }
    }

    internal static string GetMessage(this scoped ref git_tag tag)
    {
        fixed (git_tag* pTag = &tag)
        {
            var pMessage = git_tag_message(pTag);
            if (pMessage == null)
            {
                throw new InvalidOperationException($"Result is null");
            }

            var result = Marshal.PtrToStringUTF8((nint)pMessage)!;

            return result;
        }
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static List<string> GetTagList(this Lg2Repository repo, string? pattern = null)
    {
        repo.EnsureValid();

        git_strarray tags = new();

        if (pattern is not null)
        {
            var u8Pattern = new Lg2Utf8String(pattern);

            var rc = git_tag_list_match(&tags, u8Pattern.Ptr, repo.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }
        else
        {
            var rc = git_tag_list(&tags, repo.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        try
        {
            return tags.ToList();
        }
        finally
        {
            git_strarray_dispose(&tags);
        }
    }

    public static Lg2Tag LookupTag(this Lg2Repository repo, Lg2OidPlainRef oidRef)
    {
        repo.EnsureValid();

        git_tag* pTag = null;
        var rc = git_tag_lookup(&pTag, repo.Ptr, oidRef.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pTag);
    }

    public static void NewLightweightTag(
        this Lg2Repository repo,
        string tagName,
        Lg2Object target,
        bool force,
        scoped ref Lg2Oid oid
    )
    {
        repo.EnsureValid();
        target.EnsureValid();

        var u8TagName = new Lg2Utf8String(tagName);
        fixed (git_oid* ptr = &oid.Raw)
        {
            var rc = git_tag_create_lightweight(
                ptr,
                repo.Ptr,
                u8TagName.Ptr,
                target.Ptr,
                force ? 1 : 0
            );
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }

    public static void NewAnnotatedTag(
        this Lg2Repository repo,
        string tagName,
        Lg2Object target,
        Lg2SignaturePlainRef tagger,
        string message,
        bool force,
        scoped ref Lg2Oid oid
    )
    {
        repo.EnsureValid();
        target.EnsureValid();
        tagger.EnsureValid();

        var u8TagName = new Lg2Utf8String(tagName);
        var u8Message = new Lg2Utf8String(message);

        fixed (git_oid* ptr = &oid.Raw)
        {
            var rc = git_tag_create(
                ptr,
                repo.Ptr,
                u8TagName.Ptr,
                target.Ptr,
                tagger.Ptr,
                u8Message.Ptr,
                force ? 1 : 0
            );
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }
}
