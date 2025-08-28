using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using ZstdSharp;

namespace Git.Taut;

static class ProgramInfo
{
    internal const string CommandName = "git-taut";
}

static class KnownEnvironVars
{
    internal const string GitDir = "GIT_DIR";

    internal const string GitTautTrace = "GIT_TAUT_TRACE";

    internal const string GitAlternateObjectDirectories = "GIT_ALTERNATE_OBJECT_DIRECTORIES";
}

static class GitRepoHelper
{
    internal const string TautDir = "taut";
    internal const string ObjectsDir = "Objects";
    internal static readonly string ObjectsInfoDir = Path.Join(ObjectsDir, "info");
    internal static readonly string ObjectsInfoAlternatesFile = Path.Join(
        ObjectsInfoDir,
        "alternates"
    );
    internal const string DescriptionFile = "description";
    internal const string TautRemoteHelperPrefix = "taut::";
    internal const string TautCredentialSchemePrefix = "taut+";

    internal static string UseForwardSlash(string path)
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            return path.Replace('\\', '/');
        }

        return path;
    }

    internal static string GetTautDir(string repoPath)
    {
        var result = Path.Join(repoPath, TautDir);

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

    internal static string GetObjectDir(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsDir);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectsInfoAlternatesFile(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsInfoAlternatesFile);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string GetObjectInfoDir(this Lg2Repository repo)
    {
        repo.EnsureValid();

        var result = Path.Join(repo.GetPath(), ObjectsInfoDir);

        result = UseForwardSlash(result);

        return result;
    }

    internal static string AddTautRemoteHelperPrefix(string input)
    {
        if (input.StartsWith(TautRemoteHelperPrefix))
        {
            throw new ArgumentException(
                $"{input} is already prefixed with {TautRemoteHelperPrefix}"
            );
        }

        return TautRemoteHelperPrefix + input;
    }

    internal static string RemoveTautRemoteHelperPrefix(string input)
    {
        if (input.StartsWith(TautRemoteHelperPrefix) == false)
        {
            throw new ArgumentException($"{input} is not prefixed with {TautRemoteHelperPrefix}");
        }

        return input[TautRemoteHelperPrefix.Length..];
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

static class GitConfigHelper
{
    internal const string TautCredentialUrl = "tautCredentialUrl";
    internal const string TautCredentialTag = "tautCredentialTag";

    internal const string TautPattern = "tautPattern";

    internal const string Fetch_Prune = "fetch.prune";

    internal static string GetNameOfTautCredentialUrl(string remoteName)
    {
        var result = $"remote.{remoteName}.{TautCredentialUrl}";

        return result;
    }

    internal static string GetNameOfTautCredentialTag(string remoteName)
    {
        var result = $"remote.{remoteName}.{TautCredentialTag}";

        return result;
    }

    internal static string GetNameOfTautPattern(string remoteName)
    {
        var result = $"remote.{remoteName}.{TautPattern}";

        return result;
    }

    internal static string GetTautCredentialUrl(this Lg2Config config, string remoteName)
    {
        config.EnsureValid();

        var configName = GetNameOfTautCredentialUrl(remoteName);

        var result = config.GetString(configName);

        return result;
    }

    internal static string GetTautCredentialTag(this Lg2Config config, string remoteName)
    {
        config.EnsureValid();

        var configName = GetNameOfTautCredentialTag(remoteName);

        var result = config.GetString(configName);

        return result;
    }

    internal static void SetTautCredentialTag(
        this Lg2Config config,
        string remoteName,
        string value
    )
    {
        config.EnsureValid();

        var configName = GetNameOfTautCredentialTag(remoteName);

        config.SetString(configName, value);
    }

    internal static void SetTautCredentialUrl(
        this Lg2Config config,
        string remoteName,
        string value
    )
    {
        config.EnsureValid();

        var configName = GetNameOfTautCredentialUrl(remoteName);

        config.SetString(configName, value);
    }

    internal static List<string> GetTautPatterns(this Lg2Config config, string remoteName)
    {
        var name = GetNameOfTautPattern(remoteName);

        var iter = config.NewIterator(name);

        List<string> result = [];

        for (Lg2ConfigIteratorEntry? entry; iter.Next(out entry); )
        {
            var val = entry.GetValue();
            result.Add(val);
        }

        return result;
    }
}

static class AppConfigurationExtensions
{
    internal static bool GetGitTautTrace(this IConfiguration config)
    {
        var val = config[KnownEnvironVars.GitTautTrace];
        if (val is null)
        {
            return false;
        }

        if (val == "0" || val.Equals("false", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        return true;
    }
}

sealed class WrappedDecompressionStream(Stream stream, long length, bool leaveOpen = true) : Stream
{
    const int DecompressionBufferSize = 1024;
    DecompressionStream _decStream = new(
        stream,
        bufferSize: DecompressionBufferSize,
        checkEndOfStream: true,
        leaveOpen
    );
    long _length = length;
    long _totalRead;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _totalRead;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var dataRead = _decStream.Read(buffer, offset, count);

        _totalRead += dataRead;

        if (_totalRead > _length)
        {
            throw new InvalidDataException(
                $"Decompressed length ({_totalRead}) is greater than declared length ({_length})"
            );
        }

        if (dataRead == 0 && _totalRead != _length)
        {
            throw new InvalidDataException(
                $"Decompressed length ({_totalRead}) is not equal to declared length ({_length})"
            );
        }

        return dataRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    bool _isDisposed;

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        if (disposing)
        {
            _decStream.Dispose();
        }

        base.Dispose(disposing);
    }
}
