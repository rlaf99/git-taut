using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public enum Lg2ObjectType
{
    LG2_OBJECT_ANY = git_object_t.GIT_OBJECT_ANY,
    LG2_OBJECT_INVALID = git_object_t.GIT_OBJECT_INVALID,
    LG2_OBJECT_COMMIT = git_object_t.GIT_OBJECT_COMMIT,
    LG2_OBJECT_TREE = git_object_t.GIT_OBJECT_TREE,
    LG2_OBJECT_BLOB = git_object_t.GIT_OBJECT_BLOB,
    LG2_OBJECT_TAG = git_object_t.GIT_OBJECT_TAG,
    LG2_OBJECT_OFS_DELTA = git_object_t.GIT_OBJECT_OFS_DELTA,
    LG2_OBJECT_REF_DELTA = git_object_t.GIT_OBJECT_REF_DELTA,
}

public static unsafe class Lg2ObjectTypeExtensions
{
    public static bool IsValid(this Lg2ObjectType objType)
    {
        return objType == Lg2ObjectType.LG2_OBJECT_COMMIT
            || objType == Lg2ObjectType.LG2_OBJECT_TREE
            || objType == Lg2ObjectType.LG2_OBJECT_BLOB
            || objType == Lg2ObjectType.LG2_OBJECT_TAG;
    }

    public static bool IsTree(this Lg2ObjectType objType)
    {
        return objType == Lg2ObjectType.LG2_OBJECT_TREE;
    }

    public static bool IsCommit(this Lg2ObjectType objType)
    {
        return objType == Lg2ObjectType.LG2_OBJECT_COMMIT;
    }

    public static bool IsBlob(this Lg2ObjectType objType)
    {
        return objType == Lg2ObjectType.LG2_OBJECT_BLOB;
    }

    public static bool IsTag(this Lg2ObjectType objType)
    {
        return objType == Lg2ObjectType.LG2_OBJECT_TAG;
    }

    public static string GetName(this Lg2ObjectType objType)
    {
        var pStr = git_object_type2string((git_object_t)objType);
        var result = Marshal.PtrToStringUTF8((nint)pStr) ?? string.Empty;

        return result;
    }
}

[Flags]
public enum Lg2OdbLookupFlags
{
    LG2_ODB_LOOKUP_NO_REFRESH = git_odb_lookup_flags_t.GIT_ODB_LOOKUP_NO_REFRESH,
}

public enum Lg2RefType
{
    LG2_REFERENCE_INVALID = git_reference_t.GIT_REFERENCE_INVALID,
    LG2_REFERENCE_DIRECT = git_reference_t.GIT_REFERENCE_DIRECT,
    LG2_REFERENCE_SYMBOLIC = git_reference_t.GIT_REFERENCE_SYMBOLIC,
    LG2_REFERENCE_ALL = git_reference_t.GIT_REFERENCE_ALL,
}

[Flags]
public enum Lg2PathSpecFlags : uint
{
    LG2_PATHSPEC_DEFAULT = git_pathspec_flag_t.GIT_PATHSPEC_DEFAULT,
    LG2_PATHSPEC_IGNORE_CASE = git_pathspec_flag_t.GIT_PATHSPEC_IGNORE_CASE,
    LG2_PATHSPEC_USE_CASE = git_pathspec_flag_t.GIT_PATHSPEC_USE_CASE,
    LG2_PATHSPEC_NO_GLOB = git_pathspec_flag_t.GIT_PATHSPEC_NO_GLOB,
    LG2_PATHSPEC_NO_MATCH_ERROR = git_pathspec_flag_t.GIT_PATHSPEC_NO_MATCH_ERROR,
    LG2_PATHSPEC_FIND_FAILURES = git_pathspec_flag_t.GIT_PATHSPEC_FIND_FAILURES,
    LG2_PATHSPEC_FAILURES_ONLY = git_pathspec_flag_t.GIT_PATHSPEC_FAILURES_ONLY,
}

public enum Lg2FileMode
{
    LG2_FILEMODE_UNREADABLE = git_filemode_t.GIT_FILEMODE_UNREADABLE,
    LG2_FILEMODE_TREE = git_filemode_t.GIT_FILEMODE_TREE,
    LG2_FILEMODE_BLOB = git_filemode_t.GIT_FILEMODE_BLOB,
    LG2_FILEMODE_BLOB_EXECUTABLE = git_filemode_t.GIT_FILEMODE_BLOB_EXECUTABLE,
    LG2_FILEMODE_LINK = git_filemode_t.GIT_FILEMODE_LINK,
    LG2_FILEMODE_COMMIT = git_filemode_t.GIT_FILEMODE_COMMIT,
}

public enum Lg2TreeWalkMode
{
    LG2_TREEWALK_PRE = git_treewalk_mode.GIT_TREEWALK_PRE,
    LG2_TREEWALK_POST = git_treewalk_mode.GIT_TREEWALK_POST,
}
