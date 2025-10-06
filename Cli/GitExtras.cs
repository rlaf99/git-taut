using System.Text.RegularExpressions;
using Lg2.Sharpy;

namespace Git.Taut;

static class GitRepoExtra
{
    internal const string TautHomeName = "taut";
    internal const string TautCampNameTempPrefix = "__";
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

    internal static string GetTautCampPath(string repoPath, string tautCampName)
    {
        var result = Path.Join(repoPath, TautHomeName, tautCampName);

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

    internal static string GetTautCampPath(this Lg2Repository repo, string tautCampName)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), TautHomeName, tautCampName);

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

    internal static string AddTautCapNameTempPrefix(string input)
    {
        if (input.StartsWith(TautCampNameTempPrefix))
        {
            throw new ArgumentException(
                $"'{input}' is already prefixed with '{TautCampNameTempPrefix}'"
            );
        }

        return TautCampNameTempPrefix + input;
    }

    internal static string RemoveTautCampNameTempPrefix(string input, bool shouldExist = true)
    {
        if (input.StartsWith(TautCampNameTempPrefix) == false)
        {
            if (shouldExist)
            {
                throw new ArgumentException(
                    $"'{input}' is not prefixed with '{TautCampNameTempPrefix}'"
                );
            }
            else
            {
                return input;
            }
        }

        return input[TautCampNameTempPrefix.Length..];
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
}

static partial class GitConfigExtra
{
    internal const string Fetch_Prune = "fetch.prune";

    const string TautCampNameMatchPattern = $@"{TautConfig.SectionName}\.(.*)\.remote";

    [GeneratedRegex(TautCampNameMatchPattern)]
    private static partial Regex TautCampNameRegex();

    internal static bool TryFindTautCampName(
        this Lg2Config config,
        string remoteName,
        out string repoName
    )
    {
        var cfgIter = config.NewIterator(TautCampNameMatchPattern);
        Regex regex = TautCampNameRegex();

        repoName = string.Empty;
        bool found = false;

        while (cfgIter.Next(out var entry))
        {
            var val = entry.GetValue();
            if (val == remoteName)
            {
                var name = entry.GetName();
                repoName = regex.Match(name).Groups[1].Value;

                found = true;

                break;
            }
        }

        return found;
    }

    internal static bool TryFindTautCampName(
        this Lg2Repository repo,
        string remoteName,
        out string repoName
    )
    {
        repo.EnsureValid();

        using var config = repo.GetConfigSnapshot();

        return config.TryFindTautCampName(remoteName, out repoName);
    }

    internal static string FindTautCampName(this Lg2Config config, string remoteName)
    {
        if (config.TryFindTautCampName(remoteName, out var result))
        {
            return result;
        }

        throw new InvalidOperationException(
            $"Taut repo name is not found for remote '{remoteName}'"
        );
    }

    internal static string FindTautCampName(this Lg2Repository repo, string remoteName)
    {
        repo.EnsureValid();

        using var config = repo.GetConfigSnapshot();

        return config.FindTautCampName(remoteName);
    }

    internal static void PrintAllTaut(this Lg2Config config)
    {
        var tautPrefix = "taut";

        string GetSubSection(string itemName)
        {
            var part1 = itemName[(tautPrefix.Length + 1)..];
            var variableStart = part1.LastIndexOf('.');
            var part2 = part1[..variableStart];

            return part2;
        }

        HashSet<string> tautCampNames = [];

        {
            var pattern = $@"{tautPrefix}\..*";
            using var cfgIter = config.NewIterator(pattern);

            while (cfgIter.Next(out var entry))
            {
                var name = entry.GetName();

                var tautCampName = GetSubSection(name);

                tautCampNames.Add(tautCampName);
            }
        }

        foreach (var tautCampName in tautCampNames)
        {
            Console.Write($"{tautCampName}");

            {
                var pattern = $@"{tautPrefix}\.{tautCampName}\.linkTo";
                using var cfgIter = config.NewIterator(pattern);

                while (cfgIter.Next(out var entry))
                {
                    var val = entry.GetValue();

                    Console.Write($" @{val}");
                }
            }

            {
                var pattern = $@"{tautPrefix}\.{tautCampName}\.remote";
                using var cfgIter = config.NewIterator(pattern);

                while (cfgIter.Next(out var entry))
                {
                    var val = entry.GetValue();

                    Console.Write($" {val}");
                }
            }

            Console.WriteLine();
        }
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
