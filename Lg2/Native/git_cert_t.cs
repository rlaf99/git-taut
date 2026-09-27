namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_cert_t : uint
    {
        GIT_CERT_NONE,
        GIT_CERT_X509,
        GIT_CERT_HOSTKEY_LIBSSH2,
        GIT_CERT_STRARRAY,
    }
}
