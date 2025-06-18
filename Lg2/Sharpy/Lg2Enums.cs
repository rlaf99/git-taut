using System.Runtime.CompilerServices;
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

internal static unsafe class RawObjectTypeExtensions
{
    internal static Lg2ObjectType GetLg2(this git_object_t objType)
    {
        return (Lg2ObjectType)objType;
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

public enum Lg2TreeUpdateAction
{
    LG2_TREE_UPDATE_UPSERT = git_tree_update_t.GIT_TREE_UPDATE_UPSERT,
    LG2_TREE_UPDATE_REMOVE = git_tree_update_t.GIT_TREE_UPDATE_REMOVE,
}

public enum Lg2TreeWalkMode
{
    LG2_TREEWALK_PRE = git_treewalk_mode.GIT_TREEWALK_PRE,
    LG2_TREEWALK_POST = git_treewalk_mode.GIT_TREEWALK_POST,
}

[Flags]
public enum Lg2SortFlags
{
    LG2_SORT_NONE = git_sort_t.GIT_SORT_NONE,
    LG2_SORT_TOPOLOGICAL = git_sort_t.GIT_SORT_TOPOLOGICAL,
    LG2_SORT_TIME = git_sort_t.GIT_SORT_TIME,
    LG2_SORT_REVERSE = git_sort_t.GIT_SORT_REVERSE,
}

[Flags]
public enum Lg2DiffOptionFlags : uint
{
    LG2_DIFF_NORMAL = git_diff_option_t.GIT_DIFF_NORMAL,
    LG2_DIFF_REVERSE = git_diff_option_t.GIT_DIFF_REVERSE,
    LG2_DIFF_INCLUDE_IGNORED = git_diff_option_t.GIT_DIFF_INCLUDE_IGNORED,
    LG2_DIFF_RECURSE_IGNORED_DIRS = git_diff_option_t.GIT_DIFF_RECURSE_IGNORED_DIRS,
    LG2_DIFF_INCLUDE_UNTRACKED = git_diff_option_t.GIT_DIFF_INCLUDE_UNTRACKED,
    LG2_DIFF_RECURSE_UNTRACKED_DIRS = git_diff_option_t.GIT_DIFF_RECURSE_UNTRACKED_DIRS,
    LG2_DIFF_INCLUDE_UNMODIFIED = git_diff_option_t.GIT_DIFF_INCLUDE_UNMODIFIED,
    LG2_DIFF_INCLUDE_TYPECHANGE = git_diff_option_t.GIT_DIFF_INCLUDE_TYPECHANGE,
    LG2_DIFF_INCLUDE_TYPECHANGE_TREES = git_diff_option_t.GIT_DIFF_INCLUDE_TYPECHANGE_TREES,
    LG2_DIFF_IGNORE_FILEMODE = git_diff_option_t.GIT_DIFF_IGNORE_FILEMODE,
    LG2_DIFF_IGNORE_SUBMODULES = git_diff_option_t.GIT_DIFF_IGNORE_SUBMODULES,
    LG2_DIFF_IGNORE_CASE = git_diff_option_t.GIT_DIFF_IGNORE_CASE,
    LG2_DIFF_INCLUDE_CASECHANGE = git_diff_option_t.GIT_DIFF_INCLUDE_CASECHANGE,
    LG2_DIFF_DISABLE_PATHSPEC_MATCH = git_diff_option_t.GIT_DIFF_DISABLE_PATHSPEC_MATCH,
    LG2_DIFF_SKIP_BINARY_CHECK = git_diff_option_t.GIT_DIFF_SKIP_BINARY_CHECK,
    LG2_DIFF_ENABLE_FAST_UNTRACKED_DIRS = git_diff_option_t.GIT_DIFF_ENABLE_FAST_UNTRACKED_DIRS,
    LG2_DIFF_UPDATE_INDEX = git_diff_option_t.GIT_DIFF_UPDATE_INDEX,
    LG2_DIFF_INCLUDE_UNREADABLE = git_diff_option_t.GIT_DIFF_INCLUDE_UNREADABLE,
    LG2_DIFF_INCLUDE_UNREADABLE_AS_UNTRACKED =
        git_diff_option_t.GIT_DIFF_INCLUDE_UNREADABLE_AS_UNTRACKED,
    LG2_DIFF_INDENT_HEURISTIC = git_diff_option_t.GIT_DIFF_INDENT_HEURISTIC,
    LG2_DIFF_IGNORE_BLANK_LINES = git_diff_option_t.GIT_DIFF_IGNORE_BLANK_LINES,
    LG2_DIFF_FORCE_TEXT = git_diff_option_t.GIT_DIFF_FORCE_TEXT,
    LG2_DIFF_FORCE_BINARY = git_diff_option_t.GIT_DIFF_FORCE_BINARY,
    LG2_DIFF_IGNORE_WHITESPACE = git_diff_option_t.GIT_DIFF_IGNORE_WHITESPACE,
    LG2_DIFF_IGNORE_WHITESPACE_CHANGE = git_diff_option_t.GIT_DIFF_IGNORE_WHITESPACE_CHANGE,
    LG2_DIFF_IGNORE_WHITESPACE_EOL = git_diff_option_t.GIT_DIFF_IGNORE_WHITESPACE_EOL,
    LG2_DIFF_SHOW_UNTRACKED_CONTENT = git_diff_option_t.GIT_DIFF_SHOW_UNTRACKED_CONTENT,
    LG2_DIFF_SHOW_UNMODIFIED = git_diff_option_t.GIT_DIFF_SHOW_UNMODIFIED,
    LG2_DIFF_PATIENCE = git_diff_option_t.GIT_DIFF_PATIENCE,
    LG2_DIFF_MINIMAL = git_diff_option_t.GIT_DIFF_MINIMAL,
    LG2_DIFF_SHOW_BINARY = git_diff_option_t.GIT_DIFF_SHOW_BINARY,
}

[Flags]
public enum Lg2DiffFlags : uint
{
    LG2_DIFF_FLAG_BINARY = git_diff_flag_t.GIT_DIFF_FLAG_BINARY,
    LG2_DIFF_FLAG_NOT_BINARY = git_diff_flag_t.GIT_DIFF_FLAG_NOT_BINARY,
    LG2_DIFF_FLAG_VALID_ID = git_diff_flag_t.GIT_DIFF_FLAG_VALID_ID,
    LG2_DIFF_FLAG_EXISTS = git_diff_flag_t.GIT_DIFF_FLAG_EXISTS,
    LG2_DIFF_FLAG_VALID_SIZE = git_diff_flag_t.GIT_DIFF_FLAG_VALID_SIZE,
}

public enum Lg2DiffFindFlags : uint
{
    LG2_DIFF_FIND_BY_CONFIG = git_diff_find_t.GIT_DIFF_FIND_BY_CONFIG,
    LG2_DIFF_FIND_RENAMES = git_diff_find_t.GIT_DIFF_FIND_RENAMES,
    LG2_DIFF_FIND_RENAMES_FROM_REWRITES = git_diff_find_t.GIT_DIFF_FIND_RENAMES_FROM_REWRITES,
    LG2_DIFF_FIND_COPIES = git_diff_find_t.GIT_DIFF_FIND_COPIES,
    LG2_DIFF_FIND_COPIES_FROM_UNMODIFIED = git_diff_find_t.GIT_DIFF_FIND_COPIES_FROM_UNMODIFIED,
    LG2_DIFF_FIND_REWRITES = git_diff_find_t.GIT_DIFF_FIND_REWRITES,
    LG2_DIFF_BREAK_REWRITES = git_diff_find_t.GIT_DIFF_BREAK_REWRITES,
    LG2_DIFF_FIND_AND_BREAK_REWRITES = git_diff_find_t.GIT_DIFF_FIND_AND_BREAK_REWRITES,
    LG2_DIFF_FIND_FOR_UNTRACKED = git_diff_find_t.GIT_DIFF_FIND_FOR_UNTRACKED,
    LG2_DIFF_FIND_ALL = git_diff_find_t.GIT_DIFF_FIND_ALL,
    LG2_DIFF_FIND_IGNORE_LEADING_WHITESPACE =
        git_diff_find_t.GIT_DIFF_FIND_IGNORE_LEADING_WHITESPACE,
    LG2_DIFF_FIND_IGNORE_WHITESPACE = git_diff_find_t.GIT_DIFF_FIND_IGNORE_WHITESPACE,
    LG2_DIFF_FIND_DONT_IGNORE_WHITESPACE = git_diff_find_t.GIT_DIFF_FIND_DONT_IGNORE_WHITESPACE,
    LG2_DIFF_FIND_EXACT_MATCH_ONLY = git_diff_find_t.GIT_DIFF_FIND_EXACT_MATCH_ONLY,
    LG2_DIFF_BREAK_REWRITES_FOR_RENAMES_ONLY =
        git_diff_find_t.GIT_DIFF_BREAK_REWRITES_FOR_RENAMES_ONLY,
    LG2_DIFF_FIND_REMOVE_UNMODIFIED = git_diff_find_t.GIT_DIFF_FIND_REMOVE_UNMODIFIED,
}

[Flags]
public enum Lg2DeltaType
{
    LG2_DELTA_UNMODIFIED = git_delta_t.GIT_DELTA_UNMODIFIED,
    LG2_DELTA_ADDED = git_delta_t.GIT_DELTA_ADDED,
    LG2_DELTA_DELETED = git_delta_t.GIT_DELTA_DELETED,
    LG2_DELTA_MODIFIED = git_delta_t.GIT_DELTA_MODIFIED,
    LG2_DELTA_RENAMED = git_delta_t.GIT_DELTA_RENAMED,
    LG2_DELTA_COPIED = git_delta_t.GIT_DELTA_COPIED,
    LG2_DELTA_IGNORED = git_delta_t.GIT_DELTA_IGNORED,
    LG2_DELTA_UNTRACKED = git_delta_t.GIT_DELTA_UNTRACKED,
    LG2_DELTA_TYPECHANGE = git_delta_t.GIT_DELTA_TYPECHANGE,
    LG2_DELTA_UNREADABLE = git_delta_t.GIT_DELTA_UNREADABLE,
    LG2_DELTA_CONFLICTED = git_delta_t.GIT_DELTA_CONFLICTED,
}

internal static class RawDeltaTypeExtensions
{
    internal static git_delta_t GetRaw(this Lg2DeltaType deltaType)
    {
        return (git_delta_t)deltaType;
    }

    internal static Lg2DeltaType GetLg2(this git_delta_t deltaType)
    {
        return (Lg2DeltaType)deltaType;
    }
}

public static class Lg2DeltaTypeExtensions
{
    public static char GetStatusChar(this Lg2DeltaType deltaType)
    {
        var val = git_diff_status_char(deltaType.GetRaw());

        return (char)val;
    }
}
