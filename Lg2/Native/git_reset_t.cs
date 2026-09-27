namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_reset_t : uint
    {
        GIT_RESET_SOFT = 1,
        GIT_RESET_MIXED = 2,
        GIT_RESET_HARD = 3,
    }
}
