namespace Lg2.Native
{
    public unsafe partial struct git_config_backend_entry
    {
        [NativeTypeName("struct git_config_entry")]
        public git_config_entry entry;

        [NativeTypeName("void (*)(struct git_config_backend_entry *)")]
        public delegate* unmanaged[Cdecl]<git_config_backend_entry*, void> free;
    }
}
