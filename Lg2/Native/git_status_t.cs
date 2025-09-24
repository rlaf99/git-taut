namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_status_t : uint
    {
        GIT_STATUS_CURRENT = 0,
        GIT_STATUS_INDEX_NEW = (1U << 0),
        GIT_STATUS_INDEX_MODIFIED = (1U << 1),
        GIT_STATUS_INDEX_DELETED = (1U << 2),
        GIT_STATUS_INDEX_RENAMED = (1U << 3),
        GIT_STATUS_INDEX_TYPECHANGE = (1U << 4),
        GIT_STATUS_WT_NEW = (1U << 7),
        GIT_STATUS_WT_MODIFIED = (1U << 8),
        GIT_STATUS_WT_DELETED = (1U << 9),
        GIT_STATUS_WT_TYPECHANGE = (1U << 10),
        GIT_STATUS_WT_RENAMED = (1U << 11),
        GIT_STATUS_WT_UNREADABLE = (1U << 12),
        GIT_STATUS_IGNORED = (1U << 14),
        GIT_STATUS_CONFLICTED = (1U << 15),
    }
}
