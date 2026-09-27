namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_merge_preference_t : uint
    {
        GIT_MERGE_PREFERENCE_NONE = 0,
        GIT_MERGE_PREFERENCE_NO_FASTFORWARD = (1 << 0),
        GIT_MERGE_PREFERENCE_FASTFORWARD_ONLY = (1 << 1),
    }
}
