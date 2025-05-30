namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_pathspec_flag_t : uint
    {
        GIT_PATHSPEC_DEFAULT = 0,
        GIT_PATHSPEC_IGNORE_CASE = (1U << 0),
        GIT_PATHSPEC_USE_CASE = (1U << 1),
        GIT_PATHSPEC_NO_GLOB = (1U << 2),
        GIT_PATHSPEC_NO_MATCH_ERROR = (1U << 3),
        GIT_PATHSPEC_FIND_FAILURES = (1U << 4),
        GIT_PATHSPEC_FAILURES_ONLY = (1U << 5),
    }
}
