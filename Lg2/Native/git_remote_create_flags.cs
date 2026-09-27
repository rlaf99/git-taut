namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_remote_create_flags : uint
    {
        GIT_REMOTE_CREATE_SKIP_INSTEADOF = (1 << 0),
        GIT_REMOTE_CREATE_SKIP_DEFAULT_FETCHSPEC = (1 << 1),
    }
}
