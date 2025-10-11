using System.Text.RegularExpressions;
using Lg2.Sharpy;

namespace Git.Taut;

static class GitRepoHelpers
{
    internal const string TautHomeName = "taut";
    internal const string ObjectsDir = "Objects";
    internal static readonly string ObjectsInfoDir = Path.Join(ObjectsDir, "info");
    internal static readonly string ObjectsInfoAlternatesFile = Path.Join(
        ObjectsInfoDir,
        "alternates"
    );
    internal const string DescriptionFile = "description";
    internal const string TautRemoteHelperPrefix = "taut::";
    internal const string TautCredentialUrlScheme = "taut+file";

    internal static string UseForwardSlash(string somePath)
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            return somePath.Replace('\\', '/');
        }

        return somePath;
    }

    internal static string TrimEndingSlash(string somePath)
    {
        if (somePath.Length > 1 && somePath[^1] == '/')
        {
            return somePath[..^1];
        }

        return somePath;
    }

    internal static string GetTautHomePath(string repoPath)
    {
        var result = Path.Join(repoPath, TautHomeName);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetTautSitePath(string repoPath, string tautSiteName)
    {
        var result = Path.Join(repoPath, TautHomeName, tautSiteName);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetTautHomePath(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), TautHomeName);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetTautSitePath(this Lg2Repository repo, string tautSiteName)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), TautHomeName, tautSiteName);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetDescriptionFile(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), DescriptionFile);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectDirPath(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsDir);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectsInfoAlternatesFilePath(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsInfoAlternatesFile);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectInfoDirPath(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsInfoDir);

        result = UseForwardSlash(result);

        return result;
    }

    internal static bool TryGetRelativePathToWorkDir(
        this Lg2Repository repo,
        string somePath,
        out string result
    )
    {
        repo.EnsureValid();

        var fullPath = UseForwardSlash(Path.GetFullPath(somePath));
        var workDir = TrimEndingSlash(repo.GetWorkDir());

        if (fullPath.StartsWith(workDir))
        {
            var relPath = Path.GetRelativePath(workDir, fullPath);
            result = UseForwardSlash(relPath);

            return true;
        }
        else
        {
            result = string.Empty;

            return false;
        }
    }

    internal static string AddTautRemoteHelperPrefix(string input)
    {
        if (input.StartsWith(TautRemoteHelperPrefix))
        {
            throw new ArgumentException(
                $"'{input}' is already prefixed with '{TautRemoteHelperPrefix}'"
            );
        }

        return TautRemoteHelperPrefix + input;
    }

    internal static string RemoveTautRemoteHelperPrefix(string input)
    {
        if (input.StartsWith(TautRemoteHelperPrefix) == false)
        {
            throw new ArgumentException(
                $"'{input}' is not prefixed with '{TautRemoteHelperPrefix}'"
            );
        }

        return input[TautRemoteHelperPrefix.Length..];
    }

    internal static string ConvertPathToTautCredentialUrl(string somePath)
    {
        if (somePath.StartsWith(TautCredentialUrlScheme))
        {
            throw new ArgumentException(
                $"'{somePath}' already starts with '{TautCredentialUrlScheme}'"
            );
        }

        UriBuilder uriBuilder = new()
        {
            Scheme = TautCredentialUrlScheme,
            Host = "localhost",
            Path = somePath,
        };

        var result = uriBuilder.Uri.AbsoluteUri;

        return result;
    }

    internal static void DeleteGitDir(string dirPath)
    {
        foreach (var file in Directory.GetFiles(dirPath))
        {
            FileInfo info = new(file);
            info.Attributes &= ~FileAttributes.ReadOnly;
            info.Delete();
        }
        foreach (var subDirPath in Directory.GetDirectories(dirPath))
        {
            DeleteGitDir(subDirPath);
        }
        Directory.Delete(dirPath);
    }
}

static partial class GitConfigHelpers
{
    internal const string Fetch_Prune = "fetch.prune";
}

static class GitAttrHelpers
{
    internal const string TautAttrName = "taut";

    internal static Lg2AttrValue GetTautAttrValue(
        this Lg2Repository repo,
        string pathName,
        Lg2AttrOptions attrOpts
    )
    {
        repo.EnsureValid();

        var result = repo.GetAttrValue(pathName, TautAttrName, attrOpts);

        return result;
    }
}
