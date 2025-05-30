namespace Lg2.Native
{
    public partial struct git_diff_binary
    {
        [NativeTypeName("unsigned int")]
        public uint contains_data;

        public git_diff_binary_file old_file;

        public git_diff_binary_file new_file;
    }
}
