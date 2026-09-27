namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_apply_location_t : uint
    {
        GIT_APPLY_LOCATION_WORKDIR = 0,
        GIT_APPLY_LOCATION_INDEX = 1,
        GIT_APPLY_LOCATION_BOTH = 2,
    }
}
