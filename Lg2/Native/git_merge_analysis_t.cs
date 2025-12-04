namespace Lg2.Native
{
    public enum git_merge_analysis_t
    {
        GIT_MERGE_ANALYSIS_NONE = 0,
        GIT_MERGE_ANALYSIS_NORMAL = (1 << 0),
        GIT_MERGE_ANALYSIS_UP_TO_DATE = (1 << 1),
        GIT_MERGE_ANALYSIS_FASTFORWARD = (1 << 2),
        GIT_MERGE_ANALYSIS_UNBORN = (1 << 3),
    }
}
