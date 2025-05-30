namespace Lg2.Native
{
    public unsafe partial struct git_diff_line
    {
        [NativeTypeName("char")]
        public sbyte origin;

        public int old_lineno;

        public int new_lineno;

        public int num_lines;

        [NativeTypeName("size_t")]
        public nuint content_len;

        [NativeTypeName("git_off_t")]
        public long content_offset;

        [NativeTypeName("const char *")]
        public sbyte* content;
    }
}
