namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_submodule_recurse_t : uint
    {
        GIT_SUBMODULE_RECURSE_NO = 0,
        GIT_SUBMODULE_RECURSE_YES = 1,
        GIT_SUBMODULE_RECURSE_ONDEMAND = 2,
    }
}
