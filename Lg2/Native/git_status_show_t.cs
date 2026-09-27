namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_status_show_t : uint
    {
        GIT_STATUS_SHOW_INDEX_AND_WORKDIR = 0,
        GIT_STATUS_SHOW_INDEX_ONLY = 1,
        GIT_STATUS_SHOW_WORKDIR_ONLY = 2,
    }
}
