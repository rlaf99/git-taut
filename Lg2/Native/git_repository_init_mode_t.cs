namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_repository_init_mode_t : uint
    {
        GIT_REPOSITORY_INIT_SHARED_UMASK = 0,
        GIT_REPOSITORY_INIT_SHARED_GROUP = 0002775,
        GIT_REPOSITORY_INIT_SHARED_ALL = 0002777,
    }
}
