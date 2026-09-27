namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_reference_t : uint
    {
        GIT_REFERENCE_INVALID = 0,
        GIT_REFERENCE_DIRECT = 1,
        GIT_REFERENCE_SYMBOLIC = 2,
        GIT_REFERENCE_ALL = GIT_REFERENCE_DIRECT | GIT_REFERENCE_SYMBOLIC,
    }
}
