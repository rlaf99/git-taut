namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_submodule_update_t : uint
    {
        GIT_SUBMODULE_UPDATE_CHECKOUT = 1,
        GIT_SUBMODULE_UPDATE_REBASE = 2,
        GIT_SUBMODULE_UPDATE_MERGE = 3,
        GIT_SUBMODULE_UPDATE_NONE = 4,
        GIT_SUBMODULE_UPDATE_DEFAULT = 0,
    }
}
