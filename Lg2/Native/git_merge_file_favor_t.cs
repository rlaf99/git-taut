namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_merge_file_favor_t : uint
    {
        GIT_MERGE_FILE_FAVOR_NORMAL = 0,
        GIT_MERGE_FILE_FAVOR_OURS = 1,
        GIT_MERGE_FILE_FAVOR_THEIRS = 2,
        GIT_MERGE_FILE_FAVOR_UNION = 3,
    }
}
