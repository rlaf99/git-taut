namespace Lg2.Native
{
    public unsafe partial struct git_credential_ssh_key
    {
        public git_credential parent;

        [NativeTypeName("char *")]
        public sbyte* username;

        [NativeTypeName("char *")]
        public sbyte* publickey;

        [NativeTypeName("char *")]
        public sbyte* privatekey;

        [NativeTypeName("char *")]
        public sbyte* passphrase;
    }
}
