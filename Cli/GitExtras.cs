using Lg2.Sharpy;

namespace Git.Taut;

static class GitRepoHelpers
{
    internal const string HEAD = "HEAD";
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
        var workDir = TrimEndingSlash(repo.GetWorkDirectory());

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

static class GitAttrConstants
{
    internal const int DELTA_ENCODING_ENABLING_SIZE_DISABLED_VALUE = 0;
    internal const int DELTA_ENCODING_ENABLING_SIZE_DEFAULT_VALUE = 100;
    internal const int DELTA_ENCODING_ENABLING_SIZE_LOWER_BOUND = 50;

    internal const double DELTA_ENCODING_TARGET_RATIO_DISABLED_VALUE = 0.0;
    internal const double DELTA_ENCODING_TARGET_RATIO_DEFAULT_VALUE = 0.6;
    internal const double DELTA_ENCODING_TARGET_RATIO_LOWER_BOUND = 0.2;
    internal const double DELTA_ENCODING_TARGET_RATIO_UPPER_BOUND = 0.8;

    internal const double COMPRESSION_TARGET_RATIO_DISABLED_VALUE = 0.0;
    internal const double COMPRESSION_TARGET_RATIO_DEFAULT_VALUE = 0.8;
    internal const double COMPRESSION_TARGET_RATIO_LOWER_BOUND = 0.1;
    internal const double COMPRESSION_TARGET_RATIO_UPPER_BOUND = 0.9;
}

static class GitAttrHelpers
{
    internal const string TautAttrName = "taut";
    internal const string DeltaEncodingTargetRatioAttrName = "delta-encoding-target-ratio";
    internal const string DeltaEncodingEnablingSizeAttrName = "delta-encoding-enabling-size";
    internal const string CompressionTargetRatioAttrName = "target-compression-ratio";

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

    internal static Lg2AttrValue GetDeltaEncodingTargetRatioAttrValue(
        this Lg2Repository repo,
        string pathName,
        Lg2AttrOptions attrOpts
    )
    {
        repo.EnsureValid();

        var result = repo.GetAttrValue(pathName, DeltaEncodingTargetRatioAttrName, attrOpts);

        return result;
    }

    internal static Lg2AttrValue GetDeltaEncodingEnablingSizeAttrValue(
        this Lg2Repository repo,
        string pathName,
        Lg2AttrOptions attrOpts
    )
    {
        repo.EnsureValid();

        var result = repo.GetAttrValue(pathName, DeltaEncodingEnablingSizeAttrName, attrOpts);

        return result;
    }

    internal static Lg2AttrValue GetTargetCompressionRatioAttrValue(
        this Lg2Repository repo,
        string pathName,
        Lg2AttrOptions attrOpts
    )
    {
        repo.EnsureValid();

        var result = repo.GetAttrValue(pathName, CompressionTargetRatioAttrName, attrOpts);

        return result;
    }
}
