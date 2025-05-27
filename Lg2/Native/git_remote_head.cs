namespace Lg2.Native
{
    public unsafe partial struct git_remote_head
    {
        public int local;

        public git_oid oid;

        public git_oid loid;

        [NativeTypeName("char *")]
        public sbyte* name;

        [NativeTypeName("char *")]
        public sbyte* symref_target;
    }
}
