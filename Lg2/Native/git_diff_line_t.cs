namespace Lg2.Native
{
    public enum git_diff_line_t
    {
        GIT_DIFF_LINE_CONTEXT = ' ',
        GIT_DIFF_LINE_ADDITION = '+',
        GIT_DIFF_LINE_DELETION = '-',
        GIT_DIFF_LINE_CONTEXT_EOFNL = '=',
        GIT_DIFF_LINE_ADD_EOFNL = '>',
        GIT_DIFF_LINE_DEL_EOFNL = '<',
        GIT_DIFF_LINE_FILE_HDR = 'F',
        GIT_DIFF_LINE_HUNK_HDR = 'H',
        GIT_DIFF_LINE_BINARY = 'B',
    }
}
