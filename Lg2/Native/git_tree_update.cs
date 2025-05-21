namespace Lg2.Native
{
    public unsafe partial struct git_tree_update
    {
        public git_tree_update_t action;

        public git_oid id;

        public git_filemode_t filemode;

        [NativeTypeName("const char *")]
        public sbyte* path;
    }
}
