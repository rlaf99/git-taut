namespace Lg2.Native
{
    public partial struct git_fetch_options
    {
        public int version;

        public git_remote_callbacks callbacks;

        public git_fetch_prune_t prune;

        [NativeTypeName("unsigned int")]
        public uint update_fetchhead;

        public git_remote_autotag_option_t download_tags;

        public git_proxy_options proxy_opts;

        public int depth;

        public git_remote_redirect_t follow_redirects;

        public git_strarray custom_headers;
    }
}
