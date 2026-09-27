namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_diff_binary_t : uint
    {
        GIT_DIFF_BINARY_NONE,
        GIT_DIFF_BINARY_LITERAL,
        GIT_DIFF_BINARY_DELTA,
    }
}
