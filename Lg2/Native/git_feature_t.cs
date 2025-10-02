namespace Lg2.Native
{
    public enum git_feature_t
    {
        GIT_FEATURE_THREADS = (1 << 0),
        GIT_FEATURE_HTTPS = (1 << 1),
        GIT_FEATURE_SSH = (1 << 2),
        GIT_FEATURE_NSEC = (1 << 3),
        GIT_FEATURE_HTTP_PARSER = (1 << 4),
        GIT_FEATURE_REGEX = (1 << 5),
        GIT_FEATURE_I18N = (1 << 6),
        GIT_FEATURE_AUTH_NTLM = (1 << 7),
        GIT_FEATURE_AUTH_NEGOTIATE = (1 << 8),
        GIT_FEATURE_COMPRESSION = (1 << 9),
        GIT_FEATURE_SHA1 = (1 << 10),
        GIT_FEATURE_SHA256 = (1 << 11),
    }
}
