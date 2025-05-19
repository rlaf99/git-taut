namespace Lg2.Native
{
    public enum git_repository_open_flag_t
    {
        GIT_REPOSITORY_OPEN_NO_SEARCH = (1 << 0),
        GIT_REPOSITORY_OPEN_CROSS_FS = (1 << 1),
        GIT_REPOSITORY_OPEN_BARE = (1 << 2),
        GIT_REPOSITORY_OPEN_NO_DOTGIT = (1 << 3),
        GIT_REPOSITORY_OPEN_FROM_ENV = (1 << 4),
    }
}
