namespace Lg2.Native
{
    public enum git_cert_ssh_raw_type_t
    {
        GIT_CERT_SSH_RAW_TYPE_UNKNOWN = 0,
        GIT_CERT_SSH_RAW_TYPE_RSA = 1,
        GIT_CERT_SSH_RAW_TYPE_DSS = 2,
        GIT_CERT_SSH_RAW_TYPE_KEY_ECDSA_256 = 3,
        GIT_CERT_SSH_RAW_TYPE_KEY_ECDSA_384 = 4,
        GIT_CERT_SSH_RAW_TYPE_KEY_ECDSA_521 = 5,
        GIT_CERT_SSH_RAW_TYPE_KEY_ED25519 = 6,
    }
}
