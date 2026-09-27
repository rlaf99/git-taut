namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_remote_update_flags : uint
    {
        GIT_REMOTE_UPDATE_FETCHHEAD = (1 << 0),
        GIT_REMOTE_UPDATE_REPORT_UNCHANGED = (1 << 1),
    }
}
