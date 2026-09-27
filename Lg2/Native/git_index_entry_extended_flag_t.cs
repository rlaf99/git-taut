namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_index_entry_extended_flag_t : uint
    {
        GIT_INDEX_ENTRY_INTENT_TO_ADD = (1 << 13),
        GIT_INDEX_ENTRY_SKIP_WORKTREE = (1 << 14),
        GIT_INDEX_ENTRY_EXTENDED_FLAGS = (GIT_INDEX_ENTRY_INTENT_TO_ADD | GIT_INDEX_ENTRY_SKIP_WORKTREE),
        GIT_INDEX_ENTRY_UPTODATE = (1 << 2),
    }
}
