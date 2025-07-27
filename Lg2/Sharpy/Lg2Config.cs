using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public sealed unsafe class Lg2Config
    : NativeSafePointer<Lg2Config, git_config>,
        INativeRelease<git_config>
{
    public Lg2Config()
        : this(default) { }

    internal Lg2Config(git_config* pNative)
        : base(pNative) { }

    public static void NativeRelease(git_config* pNative)
    {
        git_config_free(pNative);
    }

    public static Lg2Config New()
    {
        git_config* pConfig = null;
        var rc = git_config_new(&pConfig);
        Lg2Exception.ThrowIfNotOk(rc);

        return new Lg2Config(pConfig);
    }
}

public static unsafe class Lg2ConfigExtensions
{
    public static string GetString(this Lg2Config config, string name)
    {
        config.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        git_buf buf = new();
        try
        {
            var rc = git_config_get_string_buf(&buf, config.Ptr, u8Name.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);

            var result = Marshal.PtrToStringUTF8((nint)buf.ptr);

            return result!;
        }
        finally
        {
            git_buf_dispose(&buf);
        }
    }

    public static void SetString(this Lg2Config config, string name, string value)
    {
        config.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);
        using var u8Value = new Lg2Utf8String(value);

        var rc = git_config_set_string(config.Ptr, u8Name.Ptr, u8Value.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2Config GetConfig(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_config* pConfig = null;
        var rc = git_repository_config(&pConfig, repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new Lg2Config(pConfig);
    }

    public static Lg2Config GetConfigSnapshot(this Lg2Repository repo)
    {
        repo.EnsureValid();

        git_config* pConfig = null;
        var rc = git_repository_config_snapshot(&pConfig, repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new Lg2Config(pConfig);
    }
}
