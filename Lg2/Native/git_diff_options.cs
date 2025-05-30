namespace Lg2.Native
{
    public unsafe partial struct git_diff_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint flags;

        public git_submodule_ignore_t ignore_submodules;

        public git_strarray pathspec;

        [NativeTypeName("git_diff_notify_cb")]
        public delegate* unmanaged[Cdecl]<git_diff*, git_diff_delta*, sbyte*, void*, int> notify_cb;

        [NativeTypeName("git_diff_progress_cb")]
        public delegate* unmanaged[Cdecl]<git_diff*, sbyte*, sbyte*, void*, int> progress_cb;

        public void* payload;

        [NativeTypeName("uint32_t")]
        public uint context_lines;

        [NativeTypeName("uint32_t")]
        public uint interhunk_lines;

        public git_oid_t oid_type;

        [NativeTypeName("uint16_t")]
        public ushort id_abbrev;

        [NativeTypeName("git_off_t")]
        public long max_size;

        [NativeTypeName("const char *")]
        public sbyte* old_prefix;

        [NativeTypeName("const char *")]
        public sbyte* new_prefix;
    }
}
