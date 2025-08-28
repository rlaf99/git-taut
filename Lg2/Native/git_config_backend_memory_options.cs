namespace Lg2.Native
{
    public unsafe partial struct git_config_backend_memory_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("const char *")]
        public sbyte* backend_type;

        [NativeTypeName("const char *")]
        public sbyte* origin_path;
    }
}
