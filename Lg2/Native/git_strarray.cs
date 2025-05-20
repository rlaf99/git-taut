namespace Lg2.Native
{
    public unsafe partial struct git_strarray
    {
        [NativeTypeName("char **")]
        public sbyte** strings;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
