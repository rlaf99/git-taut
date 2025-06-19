namespace Lg2.Native
{
    public partial struct git_index_time
    {
        [NativeTypeName("int32_t")]
        public int seconds;

        [NativeTypeName("uint32_t")]
        public uint nanoseconds;
    }
}
