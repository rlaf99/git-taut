namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_sort_t : uint
    {
        GIT_SORT_NONE = 0,
        GIT_SORT_TOPOLOGICAL = 1 << 0,
        GIT_SORT_TIME = 1 << 1,
        GIT_SORT_REVERSE = 1 << 2,
    }
}
