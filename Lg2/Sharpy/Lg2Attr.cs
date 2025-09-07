using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2AttrOptions
{
    public Lg2AttrOptions()
    {
        Raw = new() { version = GIT_ATTR_OPTIONS_VERSION };
    }

    internal git_attr_options Raw;

    public Lg2AttrCheckFlags Flags
    {
        get { return (Lg2AttrCheckFlags)Raw.flags; }
        set { Raw.flags = (uint)value; }
    }

    public void SetCommitId(Lg2OidPlainRef oidRef)
    {
        oidRef.EnsureValid();

        fixed (git_oid* ptr = &Raw.attr_commit_id)
        {
            var rc = git_oid_cpy(ptr, oidRef.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }
    }
}

public struct Lg2AttrValue
{
    Lg2AttrValueType _type;
    string _stringValue;

    internal Lg2AttrValue(Lg2AttrValueType type, string? stringValue = null)
    {
        _type = type;

        if (type == Lg2AttrValueType.LG2_ATTR_VALUE_STRING)
        {
            ArgumentNullException.ThrowIfNull(stringValue, nameof(stringValue));

            _stringValue = stringValue;
        }
        else
        {
            _stringValue = string.Empty;
        }
    }

    public readonly Lg2AttrValueType Type => _type;

    public bool IsSet => _type == Lg2AttrValueType.LG2_ATTR_VALUE_TRUE;
    public bool IsUnset => _type == Lg2AttrValueType.LG2_ATTR_VALUE_FALSE;
    public bool IsUnspecified => _type == Lg2AttrValueType.LG2_ATTR_VALUE_UNSPECIFIED;
    public bool IsSpecified => _type == Lg2AttrValueType.LG2_ATTR_VALUE_STRING;
    public bool IsSetOrSpecified => IsSet || IsSpecified;

    public override readonly string ToString()
    {
        return _type switch
        {
            Lg2AttrValueType.LG2_ATTR_VALUE_TRUE => "true",
            Lg2AttrValueType.LG2_ATTR_VALUE_FALSE => "false",
            Lg2AttrValueType.LG2_ATTR_VALUE_UNSPECIFIED => "unspecified",
            _ => _stringValue,
        };
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static Lg2AttrValue GetAttrValue(
        this Lg2Repository repo,
        string path,
        string name,
        Lg2AttrCheckFlags flags
    )
    {
        repo.EnsureValid();

        using var u8Path = new Lg2Utf8String(path);
        using var u8Name = new Lg2Utf8String(name);

        sbyte* ptr = null;

        var rc = git_attr_get(&ptr, repo.Ptr, (uint)flags, u8Path.Ptr, u8Name.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        var valueType = git_attr_value(ptr);

        if (valueType == git_attr_value_t.GIT_ATTR_VALUE_STRING)
        {
            var stringValue = Marshal.PtrToStringUTF8((nint)ptr);

            return new(Lg2AttrValueType.LG2_ATTR_VALUE_STRING, stringValue);
        }
        else
        {
            return new((Lg2AttrValueType)valueType);
        }
    }

    public static Lg2AttrValue GetAttrValue(
        this Lg2Repository repo,
        string path,
        string name,
        Lg2AttrOptions opts
    )
    {
        repo.EnsureValid();

        using var u8Path = new Lg2Utf8String(path);
        using var u8Name = new Lg2Utf8String(name);

        sbyte* ptr = null;

        fixed (git_attr_options* optsPtr = &opts.Raw)
        {
            var rc = git_attr_get_ext(&ptr, repo.Ptr, optsPtr, u8Path.Ptr, u8Name.Ptr);
            Lg2Exception.ThrowIfNotOk(rc);
        }

        var valueType = git_attr_value(ptr);

        if (valueType == git_attr_value_t.GIT_ATTR_VALUE_STRING)
        {
            var stringValue = Marshal.PtrToStringUTF8((nint)ptr);

            return new(Lg2AttrValueType.LG2_ATTR_VALUE_STRING, stringValue);
        }
        else
        {
            return new((Lg2AttrValueType)valueType);
        }
    }

    public static void FlushAttrCache(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var rc = git_attr_cache_flush(repo.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);
    }
}
