namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_configmap_t : uint
    {
        GIT_CONFIGMAP_FALSE = 0,
        GIT_CONFIGMAP_TRUE = 1,
        GIT_CONFIGMAP_INT32,
        GIT_CONFIGMAP_STRING,
    }
}
