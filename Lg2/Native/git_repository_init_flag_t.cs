namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_repository_init_flag_t : uint
    {
        GIT_REPOSITORY_INIT_BARE = (1U << 0),
        GIT_REPOSITORY_INIT_NO_REINIT = (1U << 1),
        GIT_REPOSITORY_INIT_NO_DOTGIT_DIR = (1U << 2),
        GIT_REPOSITORY_INIT_MKDIR = (1U << 3),
        GIT_REPOSITORY_INIT_MKPATH = (1U << 4),
        GIT_REPOSITORY_INIT_EXTERNAL_TEMPLATE = (1U << 5),
        GIT_REPOSITORY_INIT_RELATIVE_GITLINK = (1U << 6),
    }
}
