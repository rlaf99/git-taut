namespace Lg2.Native
{
    public unsafe partial struct git_status_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        public git_status_show_t show;

        [NativeTypeName("unsigned int")]
        public uint flags;

        public git_strarray pathspec;

        public git_tree* baseline;

        [NativeTypeName("uint16_t")]
        public ushort rename_threshold;
    }
}
