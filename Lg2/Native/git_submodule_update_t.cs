namespace Lg2.Native
{
    public enum git_submodule_update_t
    {
        GIT_SUBMODULE_UPDATE_CHECKOUT = 1,
        GIT_SUBMODULE_UPDATE_REBASE = 2,
        GIT_SUBMODULE_UPDATE_MERGE = 3,
        GIT_SUBMODULE_UPDATE_NONE = 4,
        GIT_SUBMODULE_UPDATE_DEFAULT = 0,
    }
}
