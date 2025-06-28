namespace Lg2.Native
{
    public enum git_cert_ssh_t
    {
        GIT_CERT_SSH_MD5 = (1 << 0),
        GIT_CERT_SSH_SHA1 = (1 << 1),
        GIT_CERT_SSH_SHA256 = (1 << 2),
        GIT_CERT_SSH_RAW = (1 << 3),
    }
}
