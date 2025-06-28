namespace Lg2.Native
{
    public unsafe partial struct git_credential_ssh_interactive
    {
        public git_credential parent;

        [NativeTypeName("char *")]
        public sbyte* username;

        [NativeTypeName("git_credential_ssh_interactive_cb")]
        public delegate* unmanaged[Cdecl]<sbyte*, int, sbyte*, int, int, _LIBSSH2_USERAUTH_KBDINT_PROMPT*, _LIBSSH2_USERAUTH_KBDINT_RESPONSE*, void**, void> prompt_callback;

        public void* payload;
    }
}
