namespace Lg2.Native
{
    public unsafe partial struct git_merge_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint flags;

        [NativeTypeName("unsigned int")]
        public uint rename_threshold;

        [NativeTypeName("unsigned int")]
        public uint target_limit;

        public git_diff_similarity_metric* metric;

        [NativeTypeName("unsigned int")]
        public uint recursion_limit;

        [NativeTypeName("const char *")]
        public sbyte* default_driver;

        public git_merge_file_favor_t file_favor;

        [NativeTypeName("uint32_t")]
        public uint file_flags;
    }
}
