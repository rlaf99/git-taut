namespace Lg2.Native
{
    public enum git_merge_flag_t
    {
        GIT_MERGE_FIND_RENAMES = (1 << 0),
        GIT_MERGE_FAIL_ON_CONFLICT = (1 << 1),
        GIT_MERGE_SKIP_REUC = (1 << 2),
        GIT_MERGE_NO_RECURSIVE = (1 << 3),
        GIT_MERGE_VIRTUAL_BASE = (1 << 4),
    }
}
