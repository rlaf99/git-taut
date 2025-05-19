namespace Lg2.Native
{
    public unsafe partial struct git_signature
    {
        [NativeTypeName("char *")]
        public sbyte* name;

        [NativeTypeName("char *")]
        public sbyte* email;

        public git_time when;
    }
}
