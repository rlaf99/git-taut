namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_credential_t : uint
    {
        GIT_CREDENTIAL_USERPASS_PLAINTEXT = (1U << 0),
        GIT_CREDENTIAL_SSH_KEY = (1U << 1),
        GIT_CREDENTIAL_SSH_CUSTOM = (1U << 2),
        GIT_CREDENTIAL_DEFAULT = (1U << 3),
        GIT_CREDENTIAL_SSH_INTERACTIVE = (1U << 4),
        GIT_CREDENTIAL_USERNAME = (1U << 5),
        GIT_CREDENTIAL_SSH_MEMORY = (1U << 6),
    }
}
