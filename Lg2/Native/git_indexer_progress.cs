namespace Lg2.Native
{
    public partial struct git_indexer_progress
    {
        [NativeTypeName("unsigned int")]
        public uint total_objects;

        [NativeTypeName("unsigned int")]
        public uint indexed_objects;

        [NativeTypeName("unsigned int")]
        public uint received_objects;

        [NativeTypeName("unsigned int")]
        public uint local_objects;

        [NativeTypeName("unsigned int")]
        public uint total_deltas;

        [NativeTypeName("unsigned int")]
        public uint indexed_deltas;

        [NativeTypeName("size_t")]
        public nuint received_bytes;
    }
}
