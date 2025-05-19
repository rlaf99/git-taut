namespace Lg2.Native
{
    public unsafe partial struct git_buf
    {
        [NativeTypeName("char *")]
        public sbyte* ptr;

        [NativeTypeName("size_t")]
        public nuint reserved;

        [NativeTypeName("size_t")]
        public nuint size;
    }
}
