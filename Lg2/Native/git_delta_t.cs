namespace Lg2.Native
{
    public enum git_delta_t
    {
        GIT_DELTA_UNMODIFIED = 0,
        GIT_DELTA_ADDED = 1,
        GIT_DELTA_DELETED = 2,
        GIT_DELTA_MODIFIED = 3,
        GIT_DELTA_RENAMED = 4,
        GIT_DELTA_COPIED = 5,
        GIT_DELTA_IGNORED = 6,
        GIT_DELTA_UNTRACKED = 7,
        GIT_DELTA_TYPECHANGE = 8,
        GIT_DELTA_UNREADABLE = 9,
        GIT_DELTA_CONFLICTED = 10,
    }
}
