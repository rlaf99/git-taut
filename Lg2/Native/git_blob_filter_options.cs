namespace Lg2.Native
{
    public unsafe partial struct git_blob_filter_options
    {
        public int version;

        [NativeTypeName("uint32_t")]
        public uint flags;

        public git_oid* commit_id;

        public git_oid attr_commit_id;
    }
}
