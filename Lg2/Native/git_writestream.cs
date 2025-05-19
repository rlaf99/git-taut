namespace Lg2.Native
{
    public unsafe partial struct git_writestream
    {
        [NativeTypeName("int (*)(git_writestream *, const char *, size_t) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_writestream*, sbyte*, nuint, int> write;

        [NativeTypeName("int (*)(git_writestream *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_writestream*, int> close;

        [NativeTypeName("void (*)(git_writestream *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_writestream*, void> free;
    }
}
