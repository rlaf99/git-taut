namespace Lg2.Native
{
    public unsafe partial struct git_index_entry
    {
        public git_index_time ctime;

        public git_index_time mtime;

        [NativeTypeName("uint32_t")]
        public uint dev;

        [NativeTypeName("uint32_t")]
        public uint ino;

        [NativeTypeName("uint32_t")]
        public uint mode;

        [NativeTypeName("uint32_t")]
        public uint uid;

        [NativeTypeName("uint32_t")]
        public uint gid;

        [NativeTypeName("uint32_t")]
        public uint file_size;

        public git_oid id;

        [NativeTypeName("uint16_t")]
        public ushort flags;

        [NativeTypeName("uint16_t")]
        public ushort flags_extended;

        [NativeTypeName("const char *")]
        public sbyte* path;
    }
}
