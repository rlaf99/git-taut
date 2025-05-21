namespace Lg2.Native
{
    public enum git_sort_t
    {
        GIT_SORT_NONE = 0,
        GIT_SORT_TOPOLOGICAL = 1 << 0,
        GIT_SORT_TIME = 1 << 1,
        GIT_SORT_REVERSE = 1 << 2,
    }
}
