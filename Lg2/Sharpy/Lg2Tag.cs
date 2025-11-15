using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Tag
    : NativeSafePointer<Lg2Tag, git_tag>,
        INativeRelease<git_tag>,
        ILg2ObjectInfo
{
    public Lg2Tag()
        : this(default) { }

    internal Lg2Tag(git_tag* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_tag* pNative)
    {
        git_tag_free(pNative);
    }

    public Lg2OidPlainRef GetOidPlainRef()
    {
        EnsureValid();
        var pOid = git_tag_id(Ptr);

        return new(pOid);
    }

    public Lg2ObjectType GetObjectType()
    {
        return Lg2ObjectType.LG2_OBJECT_TAG;
    }

    public static bool IsValidName(string name)
    {
        var u8Name = new Lg2Utf8String(name);

        int valid = 0;
        var rc = git_tag_name_is_valid(&valid, u8Name.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return valid == 0;
    }
}

public static unsafe class Lg2TagExtensions
{
    public static string GetName(this Lg2Tag tag)
    {
        tag.EnsureValid();

        var pName = git_tag_name(tag.Ptr);
        if (pName == null)
        {
            throw new InvalidOperationException($"the returned name is null");
        }

        var result = Marshal.PtrToStringUTF8((nint)pName)!;

        return result;
    }

    public static string GetMessage(this Lg2Tag tag)
    {
        tag.EnsureValid();

        var pMessage = git_tag_message(tag.Ptr);
        if (pMessage == null)
        {
            throw new InvalidOperationException($"the returned message is null");
        }

        var result = Marshal.PtrToStringUTF8((nint)pMessage)!;

        return result;
    }

    public static Lg2Object GetTarget(this Lg2Tag tag)
    {
        tag.EnsureValid();

        git_object* pObj = null;
        var rc = git_tag_target(&pObj, tag.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pObj);
    }

    public static Lg2ObjectType GetTargetType(this Lg2Tag tag)
    {
        tag.EnsureValid();

        var type = git_tag_target_type(tag.Ptr);
        return (Lg2ObjectType)type;
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Tag LookupTag(this Lg2Repository repo, scoped ref readonly Lg2Oid oid)
    {
        repo.EnsureValid();

        git_tag* pTag = null;
        fixed (git_oid* pOid = &oid.Raw)
        {
            var rc = git_tag_lookup(&pTag, repo.Ptr, pOid);
            Lg2Exception.ThrowIfNotOk(rc);
        }

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
