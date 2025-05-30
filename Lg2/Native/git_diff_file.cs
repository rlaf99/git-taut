namespace Lg2.Native
{
    public unsafe partial struct git_diff_file
    {
        public git_oid id;

        [NativeTypeName("const char *")]
        public sbyte* path;

        [NativeTypeName("git_object_size_t")]
        public ulong size;

        [NativeTypeName("uint32_t")]
        public uint flags;

        [NativeTypeName("uint16_t")]
        public ushort mode;

        [NativeTypeName("uint16_t")]
        public ushort id_abbrev;
    }
}
