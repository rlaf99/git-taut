namespace Lg2.Native
{
    public partial struct git_odb_backend_pack_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        public git_oid_t oid_type;
    }
}
