namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_diff_format_t : uint
    {
        GIT_DIFF_FORMAT_PATCH = 1U,
        GIT_DIFF_FORMAT_PATCH_HEADER = 2U,
        GIT_DIFF_FORMAT_RAW = 3U,
        GIT_DIFF_FORMAT_NAME_ONLY = 4U,
        GIT_DIFF_FORMAT_NAME_STATUS = 5U,
        GIT_DIFF_FORMAT_PATCH_ID = 6U,
    }
}
