namespace Lg2.Native
{
    public unsafe partial struct git_config_iterator
    {
        public git_config_backend* backend;

        [NativeTypeName("unsigned int")]
        public uint flags;

        [NativeTypeName("int (*)(git_config_backend_entry **, git_config_iterator *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend_entry**, git_config_iterator*, int> next;

        [NativeTypeName("void (*)(git_config_iterator *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_iterator*, void> free;
    }
}
