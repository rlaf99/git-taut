namespace Lg2.Native
{
    public unsafe partial struct git_indexer_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("git_indexer_progress_cb")]
        public delegate* unmanaged[Cdecl]<git_indexer_progress*, void*, int> progress_cb;

        public void* progress_cb_payload;

        [NativeTypeName("unsigned char")]
        public byte verify;
    }
}
