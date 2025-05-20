using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.git_error_code;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public class Lg2Exception : Exception
{
    git_error_code _errorCode = GIT_OK;

    internal Lg2Exception(git_error_code errorCode, string? message = null)
        : base(message)
    {
        _errorCode = errorCode;
    }

    internal Lg2Exception(string message)
        : base(message) { }

    internal static unsafe void RaiseIfNotOk(int code)
    {
        if (code >= 0)
        {
            return;
        }

        if (Enum.IsDefined(typeof(git_error_code), code))
        {
            var errorCode = (git_error_code)code;
            var lastError = git_error_last();
            var message = Marshal.PtrToStringUTF8((nint)lastError->message);

            throw new Lg2Exception(
                errorCode,
                $"Lg2 Error: {errorCode}: {lastError->klass}: {message}"
            );
        }
        else
        {
            throw new Lg2Exception(GIT_ERROR, $"Lg2 Error: unknown error code {code} encountered");
        }
    }
}

public unsafe class Lg2Repository : SafeHandle
{
    public Lg2Repository()
        : base(nint.Zero, true) { }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            git_repository_free((git_repository*)handle);
            handle = nint.Zero;
        }

        return true;
    }

    Lg2Repository(git_repository* repo)
        : base(nint.Zero, true)
    {
        handle = (nint)repo;
    }

    public git_repository* Ptr => (git_repository*)handle;

    public static implicit operator git_repository*(Lg2Repository repo) =>
        (git_repository*)repo.handle;

    internal static Lg2Repository Open(string repoPath)
    {
        var u8Path = new Lg2Utf8String(repoPath);

        git_repository* repo;
        var rc = git_repository_open(&repo, u8Path.Ptr);
        Lg2Exception.RaiseIfNotOk(rc);

        return new Lg2Repository(repo);
    }
}

internal static unsafe class Lg2RepositoryExtensions
{
    internal static bool IsBare(this Lg2Repository repo)
    {
        var val = git_repository_is_bare(repo.Ptr);
        return val != 0;
    }
}

public unsafe class Lg2Utf8String : SafeHandle
{
    public Lg2Utf8String(string source)
        : base(nint.Zero, true)
    {
        handle = Marshal.StringToCoTaskMemUTF8(source);
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            Marshal.FreeCoTaskMem(handle);
            handle = nint.Zero;
        }

        return true;
    }

    public sbyte* Ptr => (sbyte*)handle;

    public static implicit operator sbyte*(Lg2Utf8String str) => (sbyte*)str.handle;

    public static implicit operator Lg2Utf8String(string str) => new Lg2Utf8String(str);
}
