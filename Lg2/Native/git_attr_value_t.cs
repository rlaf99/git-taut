namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_attr_value_t : uint
    {
        GIT_ATTR_VALUE_UNSPECIFIED = 0,
        GIT_ATTR_VALUE_TRUE,
        GIT_ATTR_VALUE_FALSE,
        GIT_ATTR_VALUE_STRING,
    }
}
