namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_index_add_option_t : uint
    {
        GIT_INDEX_ADD_DEFAULT = 0,
        GIT_INDEX_ADD_FORCE = (1U << 0),
        GIT_INDEX_ADD_DISABLE_PATHSPEC_MATCH = (1U << 1),
        GIT_INDEX_ADD_CHECK_PATHSPEC = (1U << 2),
    }
}
