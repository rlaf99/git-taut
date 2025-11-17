namespace Lg2.Native
{
    /// <summary>
    /// C# does not support octal literal syntax, thus the values of git_filemode_t are specified as decimal here.
    /// </summary>
    public enum git_filemode_t
    {
        GIT_FILEMODE_UNREADABLE = 0,
        GIT_FILEMODE_TREE = 16384,
        GIT_FILEMODE_BLOB = 33188,
        GIT_FILEMODE_BLOB_EXECUTABLE = 33261,
        GIT_FILEMODE_LINK = 40960,
        GIT_FILEMODE_COMMIT = 57344,
    }
}
