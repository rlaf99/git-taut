namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_reference_format_t : uint
    {
        GIT_REFERENCE_FORMAT_NORMAL = 0U,
        GIT_REFERENCE_FORMAT_ALLOW_ONELEVEL = (1U << 0),
        GIT_REFERENCE_FORMAT_REFSPEC_PATTERN = (1U << 1),
        GIT_REFERENCE_FORMAT_REFSPEC_SHORTHAND = (1U << 2),
    }
}
