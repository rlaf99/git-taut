namespace Lg2.Native
{
    public unsafe partial struct git_merge_file_input
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("const char *")]
        public sbyte* ptr;

        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("const char *")]
        public sbyte* path;

        [NativeTypeName("unsigned int")]
        public uint mode;
    }
}
