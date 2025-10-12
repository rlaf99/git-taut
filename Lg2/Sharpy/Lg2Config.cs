using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
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

public sealed unsafe class Lg2ConfigIterator
    : NativeSafePointer<Lg2ConfigIterator, git_config_iterator>,
        INativeRelease<git_config_iterator>
{
    public Lg2ConfigIterator()
        : this(default) { }

    internal Lg2ConfigIterator(git_config_iterator* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_config_iterator* pNative)
    {
        git_config_iterator_free(pNative);
    }

    internal void AdvanceSteps()
    {
        Steps += 1;
    }

    internal int Steps { get; private set; } = 0;
}

interface ILg2ConfigEntry
{
    string GetName();
    string GetValue();
}

public sealed unsafe class Lg2ConfigEntry
    : NativeSafePointer<Lg2ConfigEntry, git_config_entry>,
        INativeRelease<git_config_entry>,
        ILg2ConfigEntry
{
    public Lg2ConfigEntry()
        : this(default) { }

    internal Lg2ConfigEntry(git_config_entry* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_config_entry* pNative)
    {
        git_config_entry_free(pNative);
    }

    public string GetName()
    {
        return Ref.GetName();
    }

    public string GetValue()
    {
        return Ref.GetValue();
    }
}

public sealed unsafe class Lg2ConfigIteratorEntry
    : NativeOwnedRef<Lg2ConfigIterator, git_config_entry>,
        ILg2ConfigEntry
{
    int _steps;

    internal Lg2ConfigIteratorEntry(Lg2ConfigIterator iter, git_config_entry* pNative)
        : base(iter, pNative)
    {
        _steps = iter.Steps;
    }

    public string GetName()
    {
        EnsureSteps();

        return (*Ptr).GetName();
    }

    public string GetValue()
    {
        EnsureSteps();

        return (*Ptr).GetValue();
    }

    void EnsureSteps()
    {
        var iter = ObtainOwner();

        if (iter.Steps != _steps)
        {
            throw new InvalidOperationException($"Iterator has already moved onto next.");
        }
    }
}

public static unsafe class Lg2ConfigExtensions
{
    public static bool TryGetString(this Lg2Config config, string name, out string value)
    {
        config.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        git_buf buf = new();
        try
        {
            var rc = git_config_get_string_buf(&buf, config.Ptr, u8Name.Ptr);
            if (rc == (int)GIT_ENOTFOUND)
            {
                value = string.Empty;
                return false;
            }

            Lg2Exception.ThrowIfNotOk(rc);

            var result = Marshal.PtrToStringUTF8((nint)buf.ptr)!;

            value = result;
            return true;
        }
        finally
        {
            git_buf_dispose(&buf);
        }
    }

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

    public static Lg2ConfigIterator NewMultiVarIterator(
        this Lg2Config config,
        string name,
        string? regExp = null
    )
    {
        config.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        git_config_iterator* ptr = null;
        if (regExp is not null)
        {
            using var u8RegExp = new Lg2Utf8String(regExp);
            var rc = git_config_multivar_iterator_new(&ptr, config.Ptr, u8Name.Ptr, u8RegExp.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }
        else
        {
            var rc = git_config_multivar_iterator_new(&ptr, config.Ptr, u8Name.Ptr, null);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        return new(ptr);
    }

    public static Lg2ConfigIterator NewIterator(this Lg2Config config, string? regExp = null)
    {
        config.EnsureValid();

        git_config_iterator* ptr = null;

        if (regExp is not null)
        {
            using var u8RegExp = new Lg2Utf8String(regExp);
            var rc = git_config_iterator_glob_new(&ptr, config.Ptr, u8RegExp.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }
        else
        {
            var rc = git_config_iterator_new(&ptr, config.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        return new(ptr);
    }

    public static void DeleteEntry(this Lg2Config config, string name)
    {
        config.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);

        var rc = git_config_delete_entry(config.Ptr, u8Name.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }

    public static void DeleteMultiVar(this Lg2Config config, string name, string regexp)
    {
        config.EnsureValid();

        using var u8Name = new Lg2Utf8String(name);
        using var u8Regexp = new Lg2Utf8String(regexp);

        var rc = git_config_delete_multivar(config.Ptr, u8Name.Ptr, u8Regexp.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}

public static unsafe class Lg2ConfigIteratorExtensions
{
    public static bool Next(
        this Lg2ConfigIterator iter,
        [NotNullWhen(true)] out Lg2ConfigIteratorEntry? result
    )
    {
        iter.EnsureValid();

        result = null;

        git_config_entry* ptr = null;
        var rc = git_config_next(&ptr, iter.Ptr);
        if (rc == (int)git_error_code.GIT_ITEROVER)
        {
            return false;
        }

        Lg2Exception.ThrowIfNotOk(rc);

        iter.AdvanceSteps();

        result = new Lg2ConfigIteratorEntry(iter, ptr);

        return true;
    }
}

public static unsafe class Lg2ConifgEntryExtensions
{
    public static string GetName(this Lg2ConfigEntry entry)
    {
        entry.EnsureValid();

        var pName = entry.Ptr->name;

        var result = Marshal.PtrToStringUTF8((nint)pName)!;

        return result;
    }

    public static string GetValue(this Lg2ConfigEntry entry)
    {
        entry.EnsureValid();

        var pValue = entry.Ptr->value;

        var result = Marshal.PtrToStringUTF8((nint)pValue)!;

        return result;
    }
}

internal static unsafe class RawConifgEntryExtensions
{
    public static string GetName(this scoped ref git_config_entry entry)
    {
        var pName = entry.name;

        var result = Marshal.PtrToStringUTF8((nint)pName)!;

        return result;
    }

    public static string GetValue(this scoped ref git_config_entry entry)
    {
        var pValue = entry.value;

        var result = Marshal.PtrToStringUTF8((nint)pValue)!;

        return result;
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
