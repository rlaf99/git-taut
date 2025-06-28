namespace Lg2.Native
{
    public partial struct git_push_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeTypeName("unsigned int")]
        public uint pb_parallelism;

        public git_remote_callbacks callbacks;

        public git_proxy_options proxy_opts;

        public git_remote_redirect_t follow_redirects;

        public git_strarray custom_headers;

        public git_strarray remote_push_options;
    }
}
