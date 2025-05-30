namespace Lg2.Native
{
    public partial struct git_diff_delta
    {
        public git_delta_t status;

        [NativeTypeName("uint32_t")]
        public uint flags;

        [NativeTypeName("uint16_t")]
        public ushort similarity;

        [NativeTypeName("uint16_t")]
        public ushort nfiles;

        public git_diff_file old_file;

        public git_diff_file new_file;
    }
}
