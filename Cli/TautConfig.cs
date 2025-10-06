using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;

namespace Git.Taut;

class TautConfig
{
    internal const string SectionName = "taut";

    internal string CampName { get; } // Sub-section name

    internal TautConfig? LinkTo { get; }

    internal string CredentialUrl { get; set; } = string.Empty;

    internal string? CredentialUserName { get; set; }

    internal string CredentialKeyTrait { get; set; } = string.Empty;

    internal List<string> RemoteNames { get; } = [];

    internal TautConfig(string tautCampName, string? tautCampNameToLink = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(tautCampName);

        CampName = tautCampName;

        if (tautCampNameToLink is not null)
        {
            LinkTo = new(tautCampNameToLink, null);
        }
    }

    string FormatTautItemName(string itemName) => $"taut.{CampName}.{itemName}";

    internal void EnsureValues()
    {
        if (string.IsNullOrEmpty(CampName))
        {
            throw new InvalidOperationException($"{nameof(CampName)} is empty");
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

        config.SetString(FormatTautItemName(nameof(LinkTo)), LinkTo.CampName);
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

    internal static bool TryLoadByTautCampName(
        Lg2Config config,
        string tautCampName,
        [NotNullWhen(true)] out TautConfig? result
    )
    {
        TautConfig tautConfig = new(tautCampName);
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
        Lg2Config config,
        string remoteName,
        [NotNullWhen(true)] out TautConfig? result
    )
    {
        if (config.TryFindTautCampName(remoteName, out var tautCampName) == false)
        {
            result = null;

            return false;
        }

        TautConfig tautConfig = new(tautCampName);
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
