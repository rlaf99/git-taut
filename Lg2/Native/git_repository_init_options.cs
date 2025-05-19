namespace Lg2.Native
{
    public unsafe partial struct git_repository_init_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint flags;

        [NativeTypeName("uint32_t")]
        public uint mode;

        [NativeTypeName("const char *")]
        public sbyte* workdir_path;

        [NativeTypeName("const char *")]
        public sbyte* description;

        [NativeTypeName("const char *")]
        public sbyte* template_path;

        [NativeTypeName("const char *")]
        public sbyte* initial_head;

        [NativeTypeName("const char *")]
        public sbyte* origin_url;
    }
}
