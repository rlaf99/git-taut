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

    /// <summary>
    /// C# does not support octal literal syntax, thus the values of git_filemode_t are specified as decimal here.
    /// </summary>
    public enum git_filemode_t
    {
        GIT_FILEMODE_UNREADABLE = 0,
        GIT_FILEMODE_TREE = 16384,
        GIT_FILEMODE_BLOB = 33188,
        GIT_FILEMODE_BLOB_EXECUTABLE = 33261,
        GIT_FILEMODE_LINK = 40960,
        GIT_FILEMODE_COMMIT = 57344,
    }
}
