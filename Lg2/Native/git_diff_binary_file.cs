namespace Lg2.Native
{
    public unsafe partial struct git_diff_binary_file
    {
        public git_diff_binary_t type;

        [NativeTypeName("const char *")]
        public sbyte* data;

        [NativeTypeName("size_t")]
        public nuint datalen;

        [NativeTypeName("size_t")]
        public nuint inflatedlen;
    }
}
