namespace Lg2.Native
{
    public unsafe partial struct git_writestream
    {
        [NativeTypeName("int (*)(git_writestream *, const char *, size_t)")]
        public delegate* unmanaged[Cdecl]<git_writestream*, sbyte*, nuint, int> write;

        [NativeTypeName("int (*)(git_writestream *)")]
        public delegate* unmanaged[Cdecl]<git_writestream*, int> close;

        [NativeTypeName("void (*)(git_writestream *)")]
        public delegate* unmanaged[Cdecl]<git_writestream*, void> free;
    }
}
