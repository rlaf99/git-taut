namespace Lg2.Native
{
    public unsafe partial struct git_remote_callbacks
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("git_transport_message_cb")]
        public delegate* unmanaged[Cdecl]<sbyte*, int, void*, int> sideband_progress;

        [NativeTypeName("int (*)(git_remote_completion_t, void *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_remote_completion_t, void*, int> completion;

        [NativeTypeName("git_credential_acquire_cb")]
        public delegate* unmanaged[Cdecl]<git_credential**, sbyte*, sbyte*, uint, void*, int> credentials;

        [NativeTypeName("git_transport_certificate_check_cb")]
        public delegate* unmanaged[Cdecl]<git_cert*, int, sbyte*, void*, int> certificate_check;

        [NativeTypeName("git_indexer_progress_cb")]
        public delegate* unmanaged[Cdecl]<git_indexer_progress*, void*, int> transfer_progress;

        [NativeTypeName("int (*)(const char *, const git_oid *, const git_oid *, void *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<sbyte*, git_oid*, git_oid*, void*, int> update_tips;

        [NativeTypeName("git_packbuilder_progress")]
        public delegate* unmanaged[Cdecl]<int, uint, uint, void*, int> pack_progress;

        [NativeTypeName("git_push_transfer_progress_cb")]
        public delegate* unmanaged[Cdecl]<uint, uint, nuint, void*, int> push_transfer_progress;

        [NativeTypeName("git_push_update_reference_cb")]
        public delegate* unmanaged[Cdecl]<sbyte*, sbyte*, void*, int> push_update_reference;

        [NativeTypeName("git_push_negotiation")]
        public delegate* unmanaged[Cdecl]<git_push_update**, nuint, void*, int> push_negotiation;

        [NativeTypeName("git_transport_cb")]
        public delegate* unmanaged[Cdecl]<git_transport**, git_remote*, void*, int> transport;

        [NativeTypeName("git_remote_ready_cb")]
        public delegate* unmanaged[Cdecl]<git_remote*, int, void*, int> remote_ready;

        public void* payload;

        [NativeTypeName("git_url_resolve_cb")]
        public delegate* unmanaged[Cdecl]<git_buf*, sbyte*, int, void*, int> resolve_url;

        [NativeTypeName("int (*)(const char *, const git_oid *, const git_oid *, git_refspec *, void *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<sbyte*, git_oid*, git_oid*, git_refspec*, void*, int> update_refs;
    }
}
