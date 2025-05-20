namespace Lg2.Native
{
    public unsafe partial struct git_error
    {
        [NativeTypeName("char *")]
        public sbyte* message;

        public int klass;
    }
}
