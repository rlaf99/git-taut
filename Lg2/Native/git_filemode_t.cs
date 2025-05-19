namespace Lg2.Native
{
    public enum git_filemode_t
    {
        GIT_FILEMODE_UNREADABLE = 0000000,
        GIT_FILEMODE_TREE = 0040000,
        GIT_FILEMODE_BLOB = 0100644,
        GIT_FILEMODE_BLOB_EXECUTABLE = 0100755,
        GIT_FILEMODE_LINK = 0120000,
        GIT_FILEMODE_COMMIT = 0160000,
    }
}
