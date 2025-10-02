namespace Lg2.Native
{
    public unsafe partial struct git_config_backend
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        public int @readonly;

        [NativeTypeName("struct git_config *")]
        public git_config* cfg;

        [NativeTypeName("int (*)(struct git_config_backend *, git_config_level_t, const git_repository *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, git_config_level_t, git_repository*, int> open;

        [NativeTypeName("int (*)(struct git_config_backend *, const char *, git_config_backend_entry **) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, sbyte*, git_config_backend_entry**, int> get;

        [NativeTypeName("int (*)(struct git_config_backend *, const char *, const char *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, sbyte*, sbyte*, int> set;

        [NativeTypeName("int (*)(git_config_backend *, const char *, const char *, const char *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, sbyte*, sbyte*, sbyte*, int> set_multivar;

        [NativeTypeName("int (*)(struct git_config_backend *, const char *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, sbyte*, int> del;

        [NativeTypeName("int (*)(struct git_config_backend *, const char *, const char *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, sbyte*, sbyte*, int> del_multivar;

        [NativeTypeName("int (*)(git_config_iterator **, struct git_config_backend *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_iterator**, git_config_backend*, int> iterator;

        [NativeTypeName("int (*)(struct git_config_backend **, struct git_config_backend *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend**, git_config_backend*, int> snapshot;

        [NativeTypeName("int (*)(struct git_config_backend *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, int> @lock;

        [NativeTypeName("int (*)(struct git_config_backend *, int) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, int, int> unlock;

        [NativeTypeName("void (*)(struct git_config_backend *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_config_backend*, void> free;
    }
}
