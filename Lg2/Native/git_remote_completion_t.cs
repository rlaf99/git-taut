namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_remote_completion_t : uint
    {
        GIT_REMOTE_COMPLETION_DOWNLOAD,
        GIT_REMOTE_COMPLETION_INDEXING,
        GIT_REMOTE_COMPLETION_ERROR,
    }
}
