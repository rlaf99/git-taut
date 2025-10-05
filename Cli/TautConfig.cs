using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;

namespace Git.Taut;

class TautConfig
{
    internal const string SectionName = "taut";

    internal string TautBaseName { get; }

    internal TautConfig? LinkTo { get; }

    internal string CredentialUrl { get; set; } = string.Empty;

    internal string? CredentialUserName { get; set; }

    internal string CredentialKeyTrait { get; set; } = string.Empty;

    internal List<string> RemoteNames { get; } = [];

    internal TautConfig(string tautBaseName, string? tautBaseNameToLink = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(tautBaseName);

        TautBaseName = tautBaseName;

        if (tautBaseNameToLink is not null)
        {
            LinkTo = new(tautBaseNameToLink, null);
        }
    }

    string FormatTautItemName(string itemName) => $"taut.{TautBaseName}.{itemName}";

    internal void EnsureValues()
    {
        if (string.IsNullOrEmpty(TautBaseName))
        {
            throw new InvalidOperationException($"{nameof(TautBaseName)} is empty");
        }

        if (string.IsNullOrEmpty(CredentialUrl))
        {
            throw new InvalidOperationException($"{nameof(CredentialUrl)} is empty");
        }

        if (string.IsNullOrEmpty(CredentialKeyTrait))
        {
            throw new InvalidOperationException($"{nameof(CredentialKeyTrait)} is empty");
        }
    }

    internal void SaveCredentialUrl(Lg2Config config)
    {
        if (string.IsNullOrEmpty(CredentialUrl))
        {
            throw new InvalidOperationException($"{nameof(CredentialUrl)} is empty");
        }

        config.SetString(FormatTautItemName(nameof(CredentialUrl)), CredentialUrl);
    }

    internal void SaveLinkTo(Lg2Config config)
    {
        if (LinkTo is null)
        {
            throw new InvalidOperationException($"{nameof(LinkTo)} is null");
        }

        config.SetString(FormatTautItemName(nameof(LinkTo)), LinkTo.TautBaseName);
    }

    internal void SaveCredentialPair(Lg2Config config)
    {
        if (string.IsNullOrEmpty(CredentialKeyTrait))
        {
            throw new InvalidOperationException($"{nameof(CredentialKeyTrait)} is empty");
        }

        if (string.IsNullOrEmpty(CredentialUserName) == false)
        {
            config.SetString(FormatTautItemName(nameof(CredentialUserName)), CredentialUserName);
        }

        config.SetString(FormatTautItemName(nameof(CredentialKeyTrait)), CredentialKeyTrait);
    }

    internal void SaveRemotes(Lg2Config config)
    {
        foreach (var remoteName in RemoteNames)
        {
            config.SetString(FormatTautItemName("remote"), remoteName);
        }
    }

    internal void Save(Lg2Config config)
    {
        EnsureValues();

        config.SetString(FormatTautItemName(nameof(CredentialUrl)), CredentialUrl);

        if (string.IsNullOrEmpty(CredentialUserName) == false)
        {
            config.SetString(FormatTautItemName(nameof(CredentialUserName)), CredentialUserName);
        }

        config.SetString(FormatTautItemName(nameof(CredentialKeyTrait)), CredentialKeyTrait);

        foreach (var remoteName in RemoteNames)
        {
            config.SetString(FormatTautItemName("remote"), remoteName);
        }
    }

    internal void Load(Lg2Config config)
    {
        CredentialUrl = config.GetString(FormatTautItemName(nameof(CredentialUrl)));

        if (
            config.TryGetString(
                FormatTautItemName(nameof(CredentialUserName)),
                out var credUserName
            )
        )
        {
            CredentialUserName = credUserName;
        }

        CredentialKeyTrait = config.GetString(FormatTautItemName(nameof(CredentialKeyTrait)));
    }

    internal static bool TryLoadByTautBaseName(
        Lg2Repository repo,
        string tautBaseName,
        [NotNullWhen(true)] out TautConfig? result
    )
    {
        using (var config = repo.GetConfigSnapshot())
        {
            return TryLoadByTautBaseName(config, tautBaseName, out result);
        }
    }

    internal static bool TryLoadByTautBaseName(
        Lg2Config config,
        string tautBaseName,
        [NotNullWhen(true)] out TautConfig? result
    )
    {
        TautConfig tautConfig = new(tautBaseName);
        try
        {
            tautConfig.Load(config);
        }
        catch (Lg2Exception)
        {
            result = null;

            return false;
        }

        result = tautConfig;

        return true;
    }

    internal static bool TryLoadByRemoteName(
        Lg2Repository repo,
        string remoteName,
        [NotNullWhen(true)] out TautConfig? result
    )
    {
        using (var config = repo.GetConfigSnapshot())
        {
            return TryLoadByRemoteName(config, remoteName, out result);
        }
    }

    internal static bool TryLoadByRemoteName(
        Lg2Config config,
        string remoteName,
        [NotNullWhen(true)] out TautConfig? result
    )
    {
        if (config.TryFindTautBaseName(remoteName, out var tautBaseName) == false)
        {
            result = null;

            return false;
        }

        TautConfig tautConfig = new(tautBaseName);
        try
        {
            tautConfig.Load(config);
        }
        catch (Lg2Exception)
        {
            result = null;

            return false;
        }

        result = tautConfig;

        return true;
    }
}
