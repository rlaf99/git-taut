using Lg2.Sharpy;

namespace Git.Taut;

static class GitRepoExtra
{
    internal const string TautHomeName = "taut";
    internal const string TautRepoNameTempPrefix = "__";
    internal const string ObjectsDir = "Objects";
    internal static readonly string ObjectsInfoDir = Path.Join(ObjectsDir, "info");
    internal static readonly string ObjectsInfoAlternatesFile = Path.Join(
        ObjectsInfoDir,
        "alternates"
    );
    internal const string DescriptionFile = "description";
    internal const string TautRemoteHelperPrefix = "taut::";
    internal const string TautCredentialSchemePrefix = "taut+";

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

    internal static string GetTautRepoPath(string repoPath, string tautRepoName)
    {
        var result = Path.Join(repoPath, TautHomeName, tautRepoName);

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

    internal static string GetTautRepoPath(this Lg2Repository repo, string remoteName)
    {
        repo.EnsureValid();

        using var config = repo.GetConfigSnapshot();

        var tautRepoName = config.GetTautRepoName(remoteName);

        if (tautRepoName is null)
        {
            throw new InvalidOperationException($"tautRepoName is null");
        }

        var result = Path.Join(repo.GetPath(), TautHomeName, tautRepoName);

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

    internal static string AddTautRepoNameTempPrefix(string input)
    {
        if (input.StartsWith(TautRepoNameTempPrefix))
        {
            throw new ArgumentException(
                $"'{input}' is already prefixed with '{TautRepoNameTempPrefix}'"
            );
        }

        return TautRepoNameTempPrefix + input;
    }

    internal static string RemoveTautRepoNameTempPrefix(string input)
    {
        if (input.StartsWith(TautRepoNameTempPrefix) == false)
        {
            throw new ArgumentException(
                $"'{input}' is not prefixed with '{TautRepoNameTempPrefix}'"
            );
        }

        return input[TautRepoNameTempPrefix.Length..];
    }

    internal static Uri ConvertToCredentialUri(Uri uri)
    {
        if (uri.Scheme.StartsWith(TautCredentialSchemePrefix))
        {
            throw new ArgumentException(
                $"{uri.Scheme} is already prefixed with {TautCredentialSchemePrefix}"
            );
        }

        if (uri.IsFile)
        {
            var uriBuilder = new UriBuilder(uri)
            {
                Scheme = TautCredentialSchemePrefix + uri.Scheme,
                Host = "localhost",
            };
            uri = uriBuilder.Uri;
        }
        else
        {
            var uriBuilder = new UriBuilder(uri)
            {
                Scheme = TautCredentialSchemePrefix + uri.Scheme,
            };
            uri = uriBuilder.Uri;
        }

        return uri;
    }

    internal static Uri ConvertHostUrlToCredentialUri(string url)
    {
        if (url.StartsWith(TautRemoteHelperPrefix) == false)
        {
            throw new ArgumentException($"Not started with {TautRemoteHelperPrefix}");
        }

        var uri = new Uri(url[TautRemoteHelperPrefix.Length..]);

        if (uri.IsFile)
        {
            var uriBuilder = new UriBuilder(uri)
            {
                Scheme = TautCredentialSchemePrefix + uri.Scheme,
                Host = "localhost",
            };
            uri = uriBuilder.Uri;
        }
        else
        {
            var uriBuilder = new UriBuilder(uri)
            {
                Scheme = TautCredentialSchemePrefix + uri.Scheme,
            };
            uri = uriBuilder.Uri;
        }

        return uri;
    }
}

static class GitConfigExtra
{
    internal const string TautRepoName = "tautRepoName";
    internal const string TautCredentialUrl = "tautCredentialUrl";
    internal const string TautCredentialUserName = "tautCredentialUserName";
    internal const string TautCredentialKeyTrait = "tautCredentialKeyTrait";

    internal const string Fetch_Prune = "fetch.prune";

    internal static string FormatRemoteItemName(string remoteName, string itemName) =>
        $"remote.{remoteName}.{itemName}";

    internal static bool TryGetTautCredentialUrl(
        this Lg2Config config,
        string remoteName,
        out string value
    ) => config.TryGetString(FormatRemoteItemName(remoteName, TautCredentialUrl), out value);

    internal static string GetTautCredentialUrl(this Lg2Config config, string remoteName) =>
        config.GetString(FormatRemoteItemName(remoteName, TautCredentialUrl));

    internal static bool TryGetTautCredentialUserName(
        this Lg2Config config,
        string remoteName,
        out string value
    ) => config.TryGetString(FormatRemoteItemName(remoteName, TautCredentialUserName), out value);

    internal static string GetTautCredentialUserName(this Lg2Config config, string remoteName) =>
        config.GetString(FormatRemoteItemName(remoteName, TautCredentialUserName));

    internal static bool TryGetTautCredentialKeyTrait(
        this Lg2Config config,
        string remoteName,
        out string value
    ) => config.TryGetString(FormatRemoteItemName(remoteName, TautCredentialKeyTrait), out value);

    internal static string GetTautCredentialKeyTrait(this Lg2Config config, string remoteName) =>
        config.GetString(FormatRemoteItemName(remoteName, TautCredentialKeyTrait));

    internal static bool TryGetTautRepoName(
        this Lg2Config config,
        string remoteName,
        out string value
    ) => config.TryGetString(FormatRemoteItemName(remoteName, TautRepoName), out value);

    internal static string GetTautRepoName(this Lg2Config config, string remoteName) =>
        config.GetString(FormatRemoteItemName(remoteName, TautRepoName));

    internal static void SetTautCredentialKeyTrait(
        this Lg2Config config,
        string remoteName,
        string value
    )
    {
        config.EnsureValid();

        var configName = FormatRemoteItemName(remoteName, TautCredentialKeyTrait);

        config.SetString(configName, value);
    }

    internal static void SetTautCredentialUserName(
        this Lg2Config config,
        string remoteName,
        string value
    )
    {
        config.EnsureValid();

        var configName = FormatRemoteItemName(remoteName, TautCredentialUserName);

        config.SetString(configName, value);
    }

    internal static void SetTautCredentialUrl(
        this Lg2Config config,
        string remoteName,
        string value
    )
    {
        config.EnsureValid();

        var configName = FormatRemoteItemName(remoteName, TautCredentialUrl);

        config.SetString(configName, value);
    }

    internal static void SetTautRepoName(this Lg2Config config, string remoteName, string value)
    {
        config.EnsureValid();

        var configName = FormatRemoteItemName(remoteName, TautRepoName);

        config.SetString(configName, value);
    }
}

static class GitAttrExtra
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
