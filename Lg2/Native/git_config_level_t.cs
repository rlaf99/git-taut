namespace Lg2.Native
{
    public enum git_config_level_t
    {
        GIT_CONFIG_LEVEL_PROGRAMDATA = 1,
        GIT_CONFIG_LEVEL_SYSTEM = 2,
        GIT_CONFIG_LEVEL_XDG = 3,
        GIT_CONFIG_LEVEL_GLOBAL = 4,
        GIT_CONFIG_LEVEL_LOCAL = 5,
        GIT_CONFIG_LEVEL_WORKTREE = 6,
        GIT_CONFIG_LEVEL_APP = 7,
        GIT_CONFIG_HIGHEST_LEVEL = -1,
    }
}
