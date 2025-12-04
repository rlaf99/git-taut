namespace Lg2.Native
{
    public enum git_merge_file_flag_t
    {
        GIT_MERGE_FILE_DEFAULT = 0,
        GIT_MERGE_FILE_STYLE_MERGE = (1 << 0),
        GIT_MERGE_FILE_STYLE_DIFF3 = (1 << 1),
        GIT_MERGE_FILE_SIMPLIFY_ALNUM = (1 << 2),
        GIT_MERGE_FILE_IGNORE_WHITESPACE = (1 << 3),
        GIT_MERGE_FILE_IGNORE_WHITESPACE_CHANGE = (1 << 4),
        GIT_MERGE_FILE_IGNORE_WHITESPACE_EOL = (1 << 5),
        GIT_MERGE_FILE_DIFF_PATIENCE = (1 << 6),
        GIT_MERGE_FILE_DIFF_MINIMAL = (1 << 7),
        GIT_MERGE_FILE_STYLE_ZDIFF3 = (1 << 8),
        GIT_MERGE_FILE_ACCEPT_CONFLICTS = (1 << 9),
    }
}
