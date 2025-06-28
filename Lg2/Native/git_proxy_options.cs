namespace Lg2.Native
{
    public unsafe partial struct git_proxy_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        public git_proxy_t type;

        [NativeTypeName("const char *")]
        public sbyte* url;

        [NativeTypeName("git_credential_acquire_cb")]
        public delegate* unmanaged[Cdecl]<git_credential**, sbyte*, sbyte*, uint, void*, int> credentials;

        [NativeTypeName("git_transport_certificate_check_cb")]
        public delegate* unmanaged[Cdecl]<git_cert*, int, sbyte*, void*, int> certificate_check;

        public void* payload;
    }
}
