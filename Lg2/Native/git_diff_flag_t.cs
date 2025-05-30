namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_diff_flag_t : uint
    {
        GIT_DIFF_FLAG_BINARY = (1U << 0),
        GIT_DIFF_FLAG_NOT_BINARY = (1U << 1),
        GIT_DIFF_FLAG_VALID_ID = (1U << 2),
        GIT_DIFF_FLAG_EXISTS = (1U << 3),
        GIT_DIFF_FLAG_VALID_SIZE = (1U << 4),
    }
}
