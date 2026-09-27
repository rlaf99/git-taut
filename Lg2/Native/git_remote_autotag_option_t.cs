namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_remote_autotag_option_t : uint
    {
        GIT_REMOTE_DOWNLOAD_TAGS_UNSPECIFIED = 0,
        GIT_REMOTE_DOWNLOAD_TAGS_AUTO,
        GIT_REMOTE_DOWNLOAD_TAGS_NONE,
        GIT_REMOTE_DOWNLOAD_TAGS_ALL,
    }
}
