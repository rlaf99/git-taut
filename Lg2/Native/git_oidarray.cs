namespace Lg2.Native
{
    public unsafe partial struct git_oidarray
    {
        public git_oid* ids;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
