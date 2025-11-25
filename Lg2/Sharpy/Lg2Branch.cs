using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

unsafe partial class Lg2Methods
{
    public static bool Lg2BranchNameIsValid(string name)
    {
        using var u8Name = new Lg2Utf8String(name);

        int valid = default;
        var rc = git_branch_name_is_valid(&valid, u8Name.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        return valid != default;
    }
}

unsafe partial class Lg2ReferenceExtensions
{
    public static string GetBranchName(this Lg2Reference reference)
    {
        reference.EnsureValid();

        sbyte* ptr = default;

        var rc = git_branch_name(&ptr, reference.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)ptr)!;

        return result;
    }

    public static bool IsHead(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var rc = git_branch_is_head(reference.Ptr);
        if (rc == 1)
        {
            return true;
        }
        else
        {
            Lg2Exception.ThrowIfNotOk(rc);

            return false;
        }
    }

    public static bool IsCheckedOut(this Lg2Reference reference)
    {
        reference.EnsureValid();

        var rc = git_branch_is_checked_out(reference.Ptr);
        if (rc == 1)
        {
            return true;
        }
        else
        {
            Lg2Exception.ThrowIfNotOk(rc);

            return false;
        }
    }
}

unsafe partial class Lg2RepositoryExtensions
{
    public static string ExtractBranchRemoteName(this Lg2Repository repo, string refName)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        git_buf buf;
        var rc = git_branch_remote_name(&buf, repo.Ptr, u8RefName.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)buf.ptr)!;

        return result;
    }

    public static string GeBranchUpstreamName(this Lg2Repository repo, string refName)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        git_buf buf;
        var rc = git_branch_upstream_name(&buf, repo.Ptr, u8RefName.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)buf.ptr)!;

        return result;
    }

    public static string GetBranchUpstreamRemoteName(this Lg2Repository repo, string refName)
    {
        repo.EnsureValid();

        using var u8RefName = new Lg2Utf8String(refName);

        git_buf buf;
        var rc = git_branch_upstream_remote(&buf, repo.Ptr, u8RefName.Ptr);
        Lg2Exception.ThrowIfNotOk(rc);

        var result = Marshal.PtrToStringUTF8((nint)buf.ptr)!;

        return result;
    }

    public static Lg2Reference LookupLocalBranch(this Lg2Repository repo, string branchName) =>
        repo.LookupBranch(branchName, Lg2BranchType.LG2_BRANCH_LOCAL);

    public static Lg2Reference LookupRemoteBranch(this Lg2Repository repo, string branchName) =>
        repo.LookupBranch(branchName, Lg2BranchType.LG2_BRANCH_REMOTE);

    public static Lg2Reference LookupAnyBranch(this Lg2Repository repo, string branchName) =>
        repo.LookupBranch(branchName, Lg2BranchType.LG2_BRANCH_ALL);

    public static Lg2Reference LookupBranch(
        this Lg2Repository repo,
        string branchName,
        Lg2BranchType branchType
    )
    {
        repo.EnsureValid();

        using var u8BranchName = new Lg2Utf8String(branchName);

        git_reference* ptr = default;
        var rc = git_branch_lookup(&ptr, repo.Ptr, u8BranchName.Ptr, (git_branch_t)branchType);
        Lg2Exception.ThrowIfNotOk(rc);

        return new(ptr);
    }
}
