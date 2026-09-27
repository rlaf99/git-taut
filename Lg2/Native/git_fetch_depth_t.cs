namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_fetch_depth_t : uint
    {
        GIT_FETCH_DEPTH_FULL = 0,
        GIT_FETCH_DEPTH_UNSHALLOW = 2147483647,
    }
}
