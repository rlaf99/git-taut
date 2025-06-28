namespace Lg2.Native
{
    public unsafe partial struct git_credential_userpass_plaintext
    {
        public git_credential parent;

        [NativeTypeName("char *")]
        public sbyte* username;

        [NativeTypeName("char *")]
        public sbyte* password;
    }
}
