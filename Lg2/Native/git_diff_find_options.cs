namespace Lg2.Native
{
    public unsafe partial struct git_diff_find_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint flags;

        [NativeTypeName("uint16_t")]
        public ushort rename_threshold;

        [NativeTypeName("uint16_t")]
        public ushort rename_from_rewrite_threshold;

        [NativeTypeName("uint16_t")]
        public ushort copy_threshold;

        [NativeTypeName("uint16_t")]
        public ushort break_rewrite_threshold;

        [NativeTypeName("size_t")]
        public nuint rename_limit;

        public git_diff_similarity_metric* metric;
    }
}
