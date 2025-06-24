using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe class Lg2Patch : NativeSafePointer<Lg2Patch, git_patch>, INativeRelease<git_patch>
{
    public Lg2Patch()
        : this(default) { }

    internal Lg2Patch(git_patch* pNative)
        : base(pNative) { }

    public static unsafe void NativeRelease(git_patch* pNative)
    {
        git_patch_free(pNative);
    }
}

public static unsafe class Lg2PatchExtensions
{
    public static nuint GetSize(
        this Lg2Patch patch,
        bool includeContext = false,
        bool includeHunkHeaders = false,
        bool includeFileHeaders = false
    )
    {
        patch.EnsureValid();

        var result = git_patch_size(
            patch.Ptr,
            includeContext ? 1 : 0,
            includeHunkHeaders ? 1 : 0,
            includeFileHeaders ? 1 : 0
        );

        return result;
    }

    public static nuint GetChunkCount(this Lg2Patch patch)
    {
        patch.EnsureValid();

        var result = git_patch_num_hunks(patch.Ptr);

        return result;
    }

    public static Lg2Buf Dump(this Lg2Patch patch)
    {
        patch.EnsureValid();

        git_buf buf = new();
        var rc = git_patch_to_buf(&buf, patch.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(buf);
    }

    public static Lg2Buf.ReadStream NewReadStream(this Lg2Patch patch)
    {
        var buf = Dump(patch);
        return buf.NewReadStream();
    }

    public static Lg2Buf Apply(this Lg2Patch patch, ReadOnlySpan<byte> diffBase)
    {
        patch.EnsureValid();

        fixed (byte* pDiffBase = diffBase)
        {
            var len = (nuint)diffBase.Length;

            git_buf buf = new();
            sbyte* pFileName = null;
            uint mode;
            var rc = git_apply_patch(
                &buf,
                &pFileName,
                &mode,
                (sbyte*)pDiffBase,
                len,
                patch.Ptr,
                null
            );
            Lg2Exception.ThrowIfNotOk(rc);

            return new(buf);
        }
    }
}

unsafe partial class Lg2DiffExtensions
{
    public static Lg2Patch NewPatch(this Lg2Diff diff, nuint idx)
    {
        diff.EnsureValid();

        git_patch* pPatch = null;
        var rc = git_patch_from_diff(&pPatch, diff.Ptr, idx);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(pPatch);
    }
}
