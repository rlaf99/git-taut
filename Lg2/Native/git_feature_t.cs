namespace Lg2.Native
{
    public enum git_feature_t
    {
        GIT_FEATURE_THREADS = (1 << 0),
        GIT_FEATURE_HTTPS = (1 << 1),
        GIT_FEATURE_SSH = (1 << 2),
        GIT_FEATURE_NSEC = (1 << 3),
    }
}
