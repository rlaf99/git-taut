namespace Lg2.Native
{
    public unsafe partial struct git_merge_file_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("const char *")]
        public sbyte* ancestor_label;

        [NativeTypeName("const char *")]
        public sbyte* our_label;

        [NativeTypeName("const char *")]
        public sbyte* their_label;

        public git_merge_file_favor_t favor;

        [NativeTypeName("uint32_t")]
        public uint flags;

        [NativeTypeName("unsigned short")]
        public ushort marker_size;
    }
}
