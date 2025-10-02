namespace Lg2.Native
{
    public unsafe partial struct git_config_entry
    {
        [NativeTypeName("const char *")]
        public sbyte* name;

        [NativeTypeName("const char *")]
        public sbyte* value;

        [NativeTypeName("const char *")]
        public sbyte* backend_type;

        [NativeTypeName("const char *")]
        public sbyte* origin_path;

        [NativeTypeName("unsigned int")]
        public uint include_depth;

        public git_config_level_t level;
    }
}
