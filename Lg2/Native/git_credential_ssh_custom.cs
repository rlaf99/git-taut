namespace Lg2.Native
{
    public unsafe partial struct git_credential_ssh_custom
    {
        public git_credential parent;

        [NativeTypeName("char *")]
        public sbyte* username;

        [NativeTypeName("char *")]
        public sbyte* publickey;

        [NativeTypeName("size_t")]
        public nuint publickey_len;

        [NativeTypeName("git_credential_sign_cb")]
        public delegate* unmanaged[Cdecl]<_LIBSSH2_SESSION*, byte**, nuint*, byte*, nuint, void**, int> sign_callback;

        public void* payload;
    }
}
