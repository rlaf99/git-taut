namespace Lg2.Native
{
    public unsafe partial struct git_configmap
    {
        public git_configmap_t type;

        [NativeTypeName("const char *")]
        public sbyte* str_match;

        public int map_value;
    }
}
