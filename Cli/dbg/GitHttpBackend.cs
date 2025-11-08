using System.Net;
using KestrelCgi;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cli.Tests.TestSupport;

class GitHttpBackend : IDisposable
{
    KestrelServer _server;
    GitHttpBackendServer _backend;

    internal Uri GetServingUri()
    {
        if (_started == false)
        {
            throw new InvalidOperationException($"Not started");
        }

        var addressFeature = _server.Features.Get<IServerAddressesFeature>();
        if (addressFeature is null)
        {
            throw new InvalidOperationException($"{nameof(addressFeature)} is null");
        }

        var address = addressFeature.Addresses.First();

        UriBuilder builder = new(address) { Path = GitHttpBackendServer.UrlPrefix };

        return builder.Uri;
    }

    internal GitHttpBackend(string repoPath, ILoggerFactory loggerFactory)
    {
        KestrelServerOptions serverOptions = new();
        serverOptions.Listen(IPAddress.Loopback, 0);

        SocketTransportOptions transportOptions = new();

        _server = new KestrelServer(
            Options.Create(serverOptions),
            new SocketTransportFactory(Options.Create(transportOptions), loggerFactory),
            loggerFactory
        );

        var logger = loggerFactory.CreateLogger<GitHttpBackendServer>();
        _backend = new GitHttpBackendServer(repoPath, logger);
    }

    bool _started;

    internal void Start()
    {
        if (_started)
        {
            throw new InvalidOperationException("Already started");
        }
        _started = true;

        Task.Run(async () =>
            {
                await _server.StartAsync(_backend, CancellationToken.None);
            })
            .GetAwaiter()
            .GetResult();
    }

    internal void Stop()
    {
        if (_started == false)
        {
            throw new InvalidOperationException("Not started");
        }
        _started = false;

        Task.Run(async () =>
            {
                await _server.StopAsync(CancellationToken.None);
            })
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}

class GitHttpBackendContext : ICgiHttpContext
{
    public required HttpContext HttpContext { get; set; }

    public bool LogErrorOutput => false;

    public TimeSpan ProcessingTimeout => TimeSpan.FromSeconds(3);
}

class GitHttpBackendServer(string repoPath, ILogger? logger = null)
    : CgiHttpApplication<GitHttpBackendContext>(logger)
{
    internal const string UrlPrefix = @"/git";

    public override GitHttpBackendContext CreateContext(IFeatureCollection contextFeatures)
    {
        GitHttpBackendContext context = new()
        {
            HttpContext = new DefaultHttpContext(contextFeatures),
        };

        return context;
    }

    public override CgiExecutionInfo? GetCgiExecutionInfo(GitHttpBackendContext context)
    {
        var request = context.HttpContext.Request;

        if (request.Path.StartsWithSegments(UrlPrefix))
        {
            const string scriptName = UrlPrefix;
            var pathInfo = request.Path.Value![scriptName.Length..];
            var envUpdate = new Dictionary<string, string>
            {
                ["GIT_PROJECT_ROOT"] = repoPath,
                ["GIT_HTTP_EXPORT_ALL"] = "1",
            };

            CgiExecutionInfo result = new(
                ScriptName: scriptName,
                PathInfo: pathInfo,
                CommandPath: "git",
                CommandArgs: ["http-backend"],
                EnvironmentUpdate: envUpdate
            );
            return result;
        }
        else
        {
            return null;
        }
    }
}
