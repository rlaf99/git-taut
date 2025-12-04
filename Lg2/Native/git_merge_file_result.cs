namespace Lg2.Native
{
    public unsafe partial struct git_merge_file_result
    {
        [NativeTypeName("unsigned int")]
        public uint automergeable;

        [NativeTypeName("const char *")]
        public sbyte* path;

        [NativeTypeName("unsigned int")]
        public uint mode;

        [NativeTypeName("const char *")]
        public sbyte* ptr;

        [NativeTypeName("size_t")]
        public nuint len;
    }
}
