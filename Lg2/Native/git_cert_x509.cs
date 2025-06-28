namespace Lg2.Native
{
    public unsafe partial struct git_cert_x509
    {
        public git_cert parent;

        public void* data;

        [NativeTypeName("size_t")]
        public nuint len;
    }
}
