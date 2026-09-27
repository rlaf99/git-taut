namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_odb_lookup_flags_t : uint
    {
        GIT_ODB_LOOKUP_NO_REFRESH = (1 << 0),
    }
}
