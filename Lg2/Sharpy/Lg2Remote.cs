using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Remote : NativeSafePointer<Lg2Remote, git_remote>, INativeRelease<git_remote>
{
    public Lg2Remote()
        : this(default) { }

    internal Lg2Remote(git_remote* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_remote* pNative)
    {
        git_remote_free(pNative);
    }
}

public static unsafe class Lg2RemoteExtensions
{
    public static string GetName(this Lg2Remote remote)
    {
        remote.EnsureValid();

        var namePtr = git_remote_name(remote.Ptr);
        if (namePtr is null)
        {
            throw new InvalidOperationException("The result is null");
        }

        var result = Marshal.PtrToStringUTF8((nint)namePtr);

        return result!;
    }

    public static string GetUrl(this Lg2Remote remote)
    {
        remote.EnsureValid();

        var urlPtr = git_remote_url(remote.Ptr);
        if (urlPtr is null)
        {
            throw new InvalidOperationException("The result is null");
        }

        var result = Marshal.PtrToStringUTF8((nint)urlPtr);

        return result!;
    }

    public static string GetPushUrl(this Lg2Remote remote)
    {
        remote.EnsureValid();

        var pushUrlPtr = git_remote_pushurl(remote.Ptr);
        if (pushUrlPtr is null)
        {
            throw new InvalidOperationException("The result is null");
        }

        var result = Marshal.PtrToStringUTF8((nint)pushUrlPtr);

        return result!;
    }

    public static void SetUrl(this Lg2Remote remote, string url)
    {
        remote.EnsureValid();

        using var u8Url = new Lg2Utf8String(url);

        var rc = git_remote_set_instance_url(remote.Ptr, u8Url.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void SetPushUrl(this Lg2Remote remote, string pushUrl)
    {
        remote.EnsureValid();

        using var u8PushUrl = new Lg2Utf8String(pushUrl);

        var rc = git_remote_set_instance_pushurl(remote.Ptr, u8PushUrl.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Remote LookupRemote(this Lg2Repository repo, string remoteName)
    {
        repo.EnsureValid();

        using var u8RemoteName = new Lg2Utf8String(remoteName);

        git_remote* ptr = null;
        var rc = git_remote_lookup(&ptr, repo.Ptr, u8RemoteName.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(ptr);
    }

    public static void SetRemoteUrl(this Lg2Repository repo, string remoteName, string url)
    {
        repo.EnsureValid();

        using var u8RemoteName = new Lg2Utf8String(remoteName);
        using var u8Url = new Lg2Utf8String(url);

        var rc = git_remote_set_url(repo.Ptr, u8RemoteName.Ptr, u8Url.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void SetRemotePushUrl(this Lg2Repository repo, string remoteName, string pushUrl)
    {
        repo.EnsureValid();

        using var u8RemoteName = new Lg2Utf8String(remoteName);
        using var u8PushUrl = new Lg2Utf8String(pushUrl);

        var rc = git_remote_set_pushurl(repo.Ptr, u8RemoteName.Ptr, u8PushUrl.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}
