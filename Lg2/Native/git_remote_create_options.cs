namespace Lg2.Native
{
    public unsafe partial struct git_remote_create_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        public git_repository* repository;

        [NativeTypeName("const char *")]
        public sbyte* name;

        [NativeTypeName("const char *")]
        public sbyte* fetchspec;

        [NativeTypeName("unsigned int")]
        public uint flags;
    }
}
