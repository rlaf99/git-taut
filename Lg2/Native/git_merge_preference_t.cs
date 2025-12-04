namespace Lg2.Native
{
    public enum git_merge_preference_t
    {
        GIT_MERGE_PREFERENCE_NONE = 0,
        GIT_MERGE_PREFERENCE_NO_FASTFORWARD = (1 << 0),
        GIT_MERGE_PREFERENCE_FASTFORWARD_ONLY = (1 << 1),
    }
}
