namespace Lg2.Native
{
    public unsafe partial struct git_checkout_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("unsigned int")]
        public uint checkout_strategy;

        public int disable_filters;

        [NativeTypeName("unsigned int")]
        public uint dir_mode;

        [NativeTypeName("unsigned int")]
        public uint file_mode;

        public int file_open_flags;

        [NativeTypeName("unsigned int")]
        public uint notify_flags;

        [NativeTypeName("git_checkout_notify_cb")]
        public delegate* unmanaged[Cdecl]<git_checkout_notify_t, sbyte*, git_diff_file*, git_diff_file*, git_diff_file*, void*, int> notify_cb;

        public void* notify_payload;

        [NativeTypeName("git_checkout_progress_cb")]
        public delegate* unmanaged[Cdecl]<sbyte*, nuint, nuint, void*, void> progress_cb;

        public void* progress_payload;

        public git_strarray paths;

        public git_tree* baseline;

        public git_index* baseline_index;

        [NativeTypeName("const char *")]
        public sbyte* target_directory;

        [NativeTypeName("const char *")]
        public sbyte* ancestor_label;

        [NativeTypeName("const char *")]
        public sbyte* our_label;

        [NativeTypeName("const char *")]
        public sbyte* their_label;

        [NativeTypeName("git_checkout_perfdata_cb")]
        public delegate* unmanaged[Cdecl]<git_checkout_perfdata*, void*, void> perfdata_cb;

        public void* perfdata_payload;
    }
}
