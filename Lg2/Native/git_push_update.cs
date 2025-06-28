namespace Lg2.Native
{
    public unsafe partial struct git_push_update
    {
        [NativeTypeName("char *")]
        public sbyte* src_refname;

        [NativeTypeName("char *")]
        public sbyte* dst_refname;

        public git_oid src;

        public git_oid dst;
    }
}
