using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2RefSpec
    : NativeSafePointer<Lg2RefSpec, git_refspec>,
        INativeRelease<git_refspec>
{
    internal Lg2RefSpec(git_refspec* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_refspec* pNative)
    {
        git_refspec_free(pNative);
    }

    static bool TryParse(string input, bool isFetch, ref Lg2RefSpec refSpec)
    {
        using var u8Input = new Lg2Utf8String(input);

        git_refspec* pRefSpec = null;
        var rc = git_refspec_parse(&pRefSpec, u8Input.Ptr, isFetch ? 1 : 0);
        if (rc != (int)GIT_OK)
        {
            return false;
        }

        refSpec.SetHandle((nint)pRefSpec);

        return true;
    }

    public static bool TryParseForPush(string input, out Lg2RefSpec refSpec)
    {
        refSpec = new(null);

        return TryParse(input, false, ref refSpec);
    }

    public static bool TryParseForFetch(string input, out Lg2RefSpec refSpec)
    {
        refSpec = new(null);

        return TryParse(input, true, ref refSpec);
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

    public static string ToString(
        this Lg2RefSpec refSpec,
        string? replaceSrc = null,
        string? replaceDst = null,
        bool? replaceForce = null
    )
    {
        var src = replaceSrc ?? refSpec.GetSrc();
        var dst = replaceDst ?? refSpec.GetDst();
        var forced = replaceForce ?? refSpec.IsForced();

        return forced ? $"+{src}:{dst}" : $"{src}:{dst}";
    }
}
