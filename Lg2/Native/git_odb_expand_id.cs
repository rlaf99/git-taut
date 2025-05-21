namespace Lg2.Native
{
    public partial struct git_odb_expand_id
    {
        public git_oid id;

        [NativeTypeName("unsigned short")]
        public ushort length;

        public git_object_t type;
    }
}
