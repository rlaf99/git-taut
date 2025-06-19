namespace Lg2.Native
{
    public unsafe partial struct git_apply_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("git_apply_delta_cb")]
        public delegate* unmanaged[Cdecl]<git_diff_delta*, void*, int> delta_cb;

        [NativeTypeName("git_apply_hunk_cb")]
        public delegate* unmanaged[Cdecl]<git_diff_hunk*, void*, int> hunk_cb;

        public void* payload;

        [NativeTypeName("unsigned int")]
        public uint flags;
    }
}
