namespace Lg2.Native
{
    public unsafe partial struct git_attr_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("unsigned int")]
        public uint flags;

        public git_oid* commit_id;

        public git_oid attr_commit_id;
    }
}
