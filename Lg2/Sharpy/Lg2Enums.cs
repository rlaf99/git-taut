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

    public static string ToString(this Lg2ObjectType objType)
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
