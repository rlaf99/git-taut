namespace Lg2.Native
{
    public partial struct git_time
    {
        [NativeTypeName("git_time_t")]
        public long time;

        public int offset;

        [NativeTypeName("char")]
        public sbyte sign;
    }
}
