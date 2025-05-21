namespace Lg2.Native
{
    public partial struct git_odb_backend_loose_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint flags;

        public int compression_level;

        [NativeTypeName("unsigned int")]
        public uint dir_mode;

        [NativeTypeName("unsigned int")]
        public uint file_mode;

        public git_oid_t oid_type;
    }
}
