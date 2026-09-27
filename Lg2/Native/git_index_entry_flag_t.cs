namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_index_entry_flag_t : uint
    {
        GIT_INDEX_ENTRY_EXTENDED = (0x4000),
        GIT_INDEX_ENTRY_VALID = (0x8000),
    }
}
