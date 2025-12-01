namespace Lg2.Native
{
    public partial struct git_checkout_perfdata
    {
        [NativeTypeName("size_t")]
        public nuint mkdir_calls;

        [NativeTypeName("size_t")]
        public nuint stat_calls;

        [NativeTypeName("size_t")]
        public nuint chmod_calls;
    }
}
