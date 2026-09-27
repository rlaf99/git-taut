namespace Lg2.Native
{
    [NativeTypeName("unsigned int")]
    public enum git_trace_level_t : uint
    {
        GIT_TRACE_NONE = 0,
        GIT_TRACE_FATAL = 1,
        GIT_TRACE_ERROR = 2,
        GIT_TRACE_WARN = 3,
        GIT_TRACE_INFO = 4,
        GIT_TRACE_DEBUG = 5,
        GIT_TRACE_TRACE = 6,
    }
}
