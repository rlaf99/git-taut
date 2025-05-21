namespace Lg2.Native
{
    public enum git_odb_stream_t
    {
        GIT_STREAM_RDONLY = (1 << 1),
        GIT_STREAM_WRONLY = (1 << 2),
        GIT_STREAM_RW = (GIT_STREAM_RDONLY | GIT_STREAM_WRONLY),
    }
}
