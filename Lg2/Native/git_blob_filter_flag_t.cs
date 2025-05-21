namespace Lg2.Native
{
    public enum git_blob_filter_flag_t
    {
        GIT_BLOB_FILTER_CHECK_FOR_BINARY = (1 << 0),
        GIT_BLOB_FILTER_NO_SYSTEM_ATTRIBUTES = (1 << 1),
        GIT_BLOB_FILTER_ATTRIBUTES_FROM_HEAD = (1 << 2),
        GIT_BLOB_FILTER_ATTRIBUTES_FROM_COMMIT = (1 << 3),
    }
}
