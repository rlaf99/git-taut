namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_diff_stats_format_t : uint
    {
        GIT_DIFF_STATS_NONE = 0,
        GIT_DIFF_STATS_FULL = (1U << 0),
        GIT_DIFF_STATS_SHORT = (1U << 1),
        GIT_DIFF_STATS_NUMBER = (1U << 2),
        GIT_DIFF_STATS_INCLUDE_SUMMARY = (1U << 3),
    }
}
