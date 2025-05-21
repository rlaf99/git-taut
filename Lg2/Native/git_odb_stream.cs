namespace Lg2.Native
{
    public unsafe partial struct git_odb_stream
    {
        public git_odb_backend* backend;

        [NativeTypeName("unsigned int")]
        public uint mode;

        public void* hash_ctx;

        [NativeTypeName("git_object_size_t")]
        public ulong declared_size;

        [NativeTypeName("git_object_size_t")]
        public ulong received_bytes;

        [NativeTypeName("int (*)(git_odb_stream *, char *, size_t) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_odb_stream*, sbyte*, nuint, int> read;

        [NativeTypeName("int (*)(git_odb_stream *, const char *, size_t) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_odb_stream*, sbyte*, nuint, int> write;

        [NativeTypeName("int (*)(git_odb_stream *, const git_oid *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_odb_stream*, git_oid*, int> finalize_write;

        [NativeTypeName("void (*)(git_odb_stream *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_odb_stream*, void> free;
    }
}
