using System.Runtime.InteropServices;

namespace Lg2.Native
{
    public static unsafe partial class LibGit2Exports
    {
        [DllImport(
            "git2-3f4182d",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        public static extern int git_apply_patch(
            git_buf* @out,
            [NativeTypeName("char **")] sbyte** filename,
            [NativeTypeName("unsigned int *")] uint* mode,
            [NativeTypeName("const char *")] sbyte* source,
            [NativeTypeName("size_t")] nuint source_len,
            git_patch* patch,
            [NativeTypeName("const git_apply_options *")] git_apply_options* opts
        );
    }
}
