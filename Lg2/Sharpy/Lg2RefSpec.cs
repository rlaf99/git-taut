using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2RefSpec
    : NativeSafePointer<Lg2RefSpec, git_refspec>,
        INativeRelease<git_refspec>
{
    public Lg2RefSpec()
        : base(default) { }

    internal Lg2RefSpec(git_refspec* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_refspec* pNative)
    {
        git_refspec_free(pNative);
    }

    public static Lg2RefSpec NewForPush(string input)
    {
        return New(input, isFetch: false);
    }

    public static Lg2RefSpec NewForFetch(string input)
    {
        return New(input, isFetch: true);
    }

    static Lg2RefSpec New(string input, bool isFetch)
    {
        using var u8Input = new Lg2Utf8String(input);

        git_refspec* pRefSpec = null;
        var rc = git_refspec_parse(&pRefSpec, u8Input.Ptr, isFetch ? 1 : 0);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pRefSpec);
    }

    static bool Parse(string input, bool isFetch, [NotNullWhen(true)] out Lg2RefSpec? refSpec)
    {
        using var u8Input = new Lg2Utf8String(input);

        git_refspec* pRefSpec = null;
        var rc = git_refspec_parse(&pRefSpec, u8Input.Ptr, isFetch ? 1 : 0);
        if (rc != (int)GIT_OK)
        {
            refSpec = default;

            return false;
        }
        else
        {
            refSpec = new(pRefSpec);

            return true;
        }
    }

    public static bool TryParseForPush(string input, out Lg2RefSpec refSpec)
    {
        var success = Parse(input, isFetch: false, out var rs);
        refSpec = success ? rs! : new();
        return success;
    }

    public static bool TryParseForFetch(string input, out Lg2RefSpec refSpec)
    {
        var success = Parse(input, isFetch: true, out var rs);
        refSpec = success ? rs! : new();
        return success;
    }
}

public static unsafe class Lg2RefSpecExtensions
{
    public static string GetSrc(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var pSrc = git_refspec_src(refSpec.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pSrc) ?? string.Empty;

        return result;
    }

    public static string GetDst(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var pDst = git_refspec_dst(refSpec.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pDst) ?? string.Empty;

        return result;
    }

    public static string GetString(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var pStr = git_refspec_string(refSpec.Ptr);
        var result = Marshal.PtrToStringUTF8((nint)pStr) ?? string.Empty;

        return result;
    }

    public static bool IsForced(this Lg2RefSpec refSpec)
    {
        refSpec.EnsureValid();

        var force = git_refspec_force(refSpec.Ptr);

        return force != 0;
    }

    static string Transform(this Lg2RefSpec refSpec, string refName, bool reverse)
    {
        refSpec.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        git_buf buf = new();

        try
        {
            int rc;
            if (reverse)
            {
                rc = git_refspec_rtransform(&buf, refSpec.Ptr, u8RefName.Ptr);
            }
            else
            {
                rc = git_refspec_transform(&buf, refSpec.Ptr, u8RefName.Ptr);
            }
            Lg2Exception.ThrowIfNotOk(rc);

            var result = string.Empty;

            if (buf.size > 0)
            {
                result = Marshal.PtrToStringUTF8((nint)buf.ptr) ?? string.Empty;
            }

            return result;
        }
        finally
        {
            git_buf_dispose(&buf);
        }
    }

    public static string TransformToTarget(this Lg2RefSpec refSpec, string refName)
    {
        return refSpec.Transform(refName, reverse: false);
    }

    public static string TransformToSource(this Lg2RefSpec refSpec, string refName)
    {
        return refSpec.Transform(refName, reverse: true);
    }

    public static bool SrcMatches(this Lg2RefSpec refSpec, string refName)
    {
        refSpec.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        var result = git_refspec_src_matches(refSpec.Ptr, u8RefName.Ptr);

        return result != 0;
    }

    public static bool DstMatches(this Lg2RefSpec refSpec, string refName)
    {
        refSpec.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        var result = git_refspec_dst_matches(refSpec.Ptr, u8RefName.Ptr);

        return result != 0;
    }

    public static string ToString(
        this Lg2RefSpec refSpec,
        string? replaceSrc = null,
        string? replaceDst = null,
        bool? replaceForce = null
    )
    {
        refSpec.EnsureValid();

        var src = replaceSrc ?? refSpec.GetSrc();
        var dst = replaceDst ?? refSpec.GetDst();
        var forced = replaceForce ?? refSpec.IsForced();

        return forced ? $"+{src}:{dst}" : $"{src}:{dst}";
    }
}
