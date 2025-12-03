using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;
using static Lg2.Sharpy.Lg2Methods;

namespace Git.Taut;

sealed class GitTautHostBuilder
{
    public static IHost BuildHost()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddForGitTaut();

        return builder.Build();
    }
}

sealed class GitRemoteTautHostBuilder
{
    public static IHost BuildHost()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddForGitRemoteTaut();

        return builder.Build();
    }
}

static class HostApplicationBuilderExtensions
{
    internal static void AddForGitTaut(this HostApplicationBuilder builder)
    {
        builder.AddGitTautConfiguration();
        builder.AddGitTautServices();
        builder.AddGitTautCommandActions();
        builder.AddGitTautLogging();
    }

    internal static void AddForGitRemoteTaut(this HostApplicationBuilder builder)
    {
        builder.AddGitTautConfiguration();
        builder.AddGitTautServices();
        builder.AddGitRemoteTautCommandActions();
        builder.AddGitTautLogging();
    }

    internal static void AddGitTautCommandActions(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<CommandActionHelpers>();
        builder.Services.AddSingleton<SiteCommandActions>();
    }

    internal static void AddGitRemoteTautCommandActions(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<CommandActionHelpers>();
        builder.Services.AddSingleton<GitRemoteHelper>();
    }

    internal static void AddGitTautConfiguration(this HostApplicationBuilder builder)
    {
        var config = builder.Configuration;

        config.AddEnvironmentVariables();
    }

    internal static void AddGitTautServices(this HostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddSingleton<GitCli>();
        services.AddSingleton<TautSetup>();
        services.AddSingleton<TautManager>();
        services.AddSingleton<TautMapping>();
        services.AddSingleton<TautAttributes>();
        services.AddSingleton<Aes256Cbc1>();
        services.AddSingleton<RecyclableMemoryStreamManager>();

#if DEBUG
        services.AddSingleton<GitHttpBackendCommandBuilder>();
        services.AddSingleton<GitSshBypassCommandBuilder>();
#endif
    }

    internal static void AddGitTautLogging(this HostApplicationBuilder builder)
    {
        var logging = builder.Logging;

        logging.ClearProviders();

        logging.AddZLoggerConsole(options =>
        {
            // log all to standard error
            options.LogToStandardErrorThreshold = LogLevel.Trace;

            options.UsePlainTextFormatter(formatter =>
            {
                formatter.SetPrefixFormatter(
                    $"{0} {1}[{2:short}]\t\t",
                    (in MessageTemplate template, in LogInfo info) =>
                        template.Format(
                            info.Timestamp.Local.ToString("hh:mm:ss.ffffff"),
                            AppInfo.GitTautCommandName,
                            info.LogLevel
                        )
                );

                formatter.SetExceptionFormatter(
                    (writer, ex) =>
                        Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}")
                );
            });

            options.FullMode = BackgroundBufferFullMode.Block;
        });

        if (builder.Configuration.GetGitTautTrace())
        {
            logging.SetMinimumLevel(LogLevel.Trace);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Information);
        }
    }
}

class CommandActionHelpers
{
    internal record ResolveTargetOptionResult(
        string SiteName,
        string? RemoteName,
        bool TargetIsRemote,
        bool SiteIsFromHead
    );

    internal ResolveTargetOptionResult ResolveTargetOption(
        ParseResult parseResult,
        Lg2Repository hostRepo,
        bool followHead
    )
    {
        var targetName = parseResult.GetValue(ProgramCommandLine.SiteTargetOption);
        if (targetName is not null)
        {
            using var config = hostRepo.GetConfigSnapshot();

            string? siteName = null;
            bool targetIsRemote = false;

            if (TautSiteConfiguration.IsExistingSite(config, targetName))
            {
                siteName = targetName;
            }
            else
            {
                if (hostRepo.TryLookupRemote(targetName, out var remote))
                {
                    targetIsRemote = TautSiteConfiguration.TryFindSiteNameForRemote(
                        config,
                        remote,
                        out siteName
                    );
                }
            }

            if (siteName is null)
            {
                throw new InvalidOperationException(
                    $"The value '{targetName}' specified by {ProgramCommandLine.SiteTargetOption.Name} is invalid"
                );
            }

            var siteConfig = TautSiteConfiguration.LoadNew(config, siteName);

            return new(
                SiteName: siteConfig.SiteName,
                RemoteName: targetIsRemote ? targetName : null,
                TargetIsRemote: targetIsRemote,
                SiteIsFromHead: false
            );
        }
        else if (followHead)
        {
            if (TryResolveTautSiteNameFromHead(hostRepo, out var tautSiteName))
            {
                return new(
                    SiteName: tautSiteName,
                    RemoteName: null,
                    TargetIsRemote: false,
                    SiteIsFromHead: true
                );
            }

            throw new InvalidOperationException($"Cannot resolve taut site name from HEAD");
        }
        else
        {
            throw new InvalidOperationException(
                $"Option {ProgramCommandLine.SiteTargetOption.Name} is not specified"
            );
        }
    }

    internal bool TryResolveTautSiteNameFromHead(
        Lg2Repository hostRepo,
        [NotNullWhen(true)] out string? tautSiteName
    )
    {
        var repoHead = hostRepo.GetHead();
        if (repoHead.IsBranch() == false)
        {
            tautSiteName = null;
            return false;
        }

        var headRefName = repoHead.GetName();
        var remoteName = hostRepo.GetBranchUpstreamRemoteName(headRefName);
        var remote = hostRepo.LookupRemote(remoteName);

        using (var config = hostRepo.GetConfigSnapshot())
        {
            if (TautSiteConfiguration.TryLoadForRemote(config, remote, out var tautConfig))
            {
                tautSiteName = tautConfig.SiteName;

                return true;
            }
        }

        tautSiteName = null;
        return false;
    }

    internal Lg2Repository LocateHostRepo()
    {
        if (AppEnvironment.TryGetGitDir(out var gitDir))
        {
            return Lg2Repository.New(gitDir);
        }

        var launchDir = LaunchDirectory ?? Directory.GetCurrentDirectory();

        var ceilingDirs = AppEnvironment.GetGitCeilingDirectories();

        if (Lg2TryDiscoverRepository(launchDir, out var hostRepo, ceilingDirs) == false)
        {
            throw new InvalidOperationException($"Not inside a git repository");
        }

        return hostRepo;
    }

    internal string ResolveLocalUrl(string remoteAddress)
    {
        string remoteUrl;

        if (Directory.Exists(remoteAddress))
        {
            using var repo = Lg2Repository.New(
                remoteAddress,
                Lg2RepositoryOpenFlags.LG2_REPOSITORY_OPEN_NO_SEARCH
            );

            remoteUrl = repo.GetPath();
        }
        else if (remoteAddress.Contains(':'))
        {
            remoteUrl = remoteAddress;
        }
        else
        {
            throw new InvalidOperationException($"Unrecoginized repository path '{remoteAddress}'");
        }

        return remoteUrl;
    }

    internal string? LaunchDirectory { get; set; }
}

class SiteCommandActions(
    ILogger<SiteCommandActions> logger,
    GitCli gitCli,
    TautSetup tautSetup,
    TautManager tautManager,
    TautMapping tautMapping,
    Aes256Cbc1 cipher,
    CommandActionHelpers actionHelpers
)
{
    internal void Run(ParseResult parseResult)
    {
        var cmdName = parseResult.GetRequiredValue(ProgramCommandLine.CommandNameArgument);
        var cmdArgs = parseResult.GetValue(ProgramCommandLine.CommandArgsArgument) ?? [];

        var hostRepo = actionHelpers.LocateHostRepo();

        var result = actionHelpers.ResolveTargetOption(parseResult, hostRepo, followHead: true);

        tautSetup.GearUpExisting(hostRepo, remoteName: null, result.SiteName);

        using (tautSetup)
        {
            string[] gitArgs = ["--git-dir", tautSetup.TautRepo.GetPath(), cmdName, .. cmdArgs];
            try
            {
                gitCli.Run(gitArgs);
            }
            catch (Exception)
            {
                Console.Error.WriteLine($"Failed to run `{string.Join(" ", gitArgs)}`");
            }
        }
    }

    internal Task<int> PerformAction(
        ParseResult parseResult,
        CancellationToken cancellation,
        Action<ParseResult> action,
        string actionName
    )
    {
        logger.ZLogTrace($"Start invoking action {actionName}");

        var error = parseResult.InvocationConfiguration.Error;

        try
        {
            action(parseResult);
        }
        catch (InvalidOperationException ex)
        {
            error.WriteLine(ex.Message);

            return Task.FromResult(1);
        }

        logger.ZLogTrace($"Done invoking action {actionName}");

        return Task.FromResult(0);
    }

    internal void Add(ParseResult parseResult)
    {
        var remoteName = parseResult.GetRequiredValue(ProgramCommandLine.RemoteNameArgument);
        var remoteAddress = parseResult.GetRequiredValue(ProgramCommandLine.RemoteAddressArgument);

        var linkExisting = parseResult.GetValue(ProgramCommandLine.LinkExistingOption);

        var hostRepo = actionHelpers.LocateHostRepo();

        if (hostRepo.TryLookupRemote(remoteName, out _))
        {
            throw new InvalidOperationException(
                $"Remote '{remoteName}' already exists in the host repository"
            );
        }

        string? targetSite = null;
        if (parseResult.GetValue(ProgramCommandLine.SiteTargetOption) is not null)
        {
            var result = actionHelpers.ResolveTargetOption(
                parseResult,
                hostRepo,
                followHead: false
            );
            targetSite = result.SiteName;
        }

        if (linkExisting && targetSite is null)
        {
            throw new InvalidOperationException(
                $"Option {ProgramCommandLine.SiteTargetOption.Name} is not speficied when {ProgramCommandLine.LinkExistingOption.Name} is used"
            );
        }

        if (linkExisting && targetSite is not null)
        {
            using var config = hostRepo.GetConfigSnapshot();
            var targetSiteConfig = TautSiteConfiguration.LoadNew(config, targetSite);

            if (targetSiteConfig.LinkTo is not null)
            {
                throw new InvalidOperationException(
                    $"Cannot link to a site '{targetSite}' that already links to other site"
                );
            }
        }

        string remoteUrl = actionHelpers.ResolveLocalUrl(remoteAddress);

        tautSetup.GearUpBrandNew(hostRepo, remoteName, remoteUrl, targetSite);

        tautManager.RegainHeads();

        tautSetup.WrapUpBrandNew();
    }

    internal void List(ParseResult parseResult)
    {
        var hostRepo = actionHelpers.LocateHostRepo();

        string? tautSiteName = null;

        if (parseResult.GetValue(ProgramCommandLine.SiteTargetOption) is not null)
        {
            var result = actionHelpers.ResolveTargetOption(
                parseResult,
                hostRepo,
                followHead: false
            );
            tautSiteName = result.SiteName;
        }

        var outputWriter = parseResult.InvocationConfiguration.Output;

        using (var config = hostRepo.GetConfigSnapshot())
        {
            TautSiteConfiguration.PrintSites(config, outputWriter, tautSiteName);
        }
    }

    internal void Remove(ParseResult parseResult)
    {
        var hostRepo = actionHelpers.LocateHostRepo();

        var resolvedTarget = actionHelpers.ResolveTargetOption(
            parseResult,
            hostRepo,
            followHead: false
        );
        var tautSiteName = resolvedTarget.SiteName;

        using (var config = hostRepo.GetConfig())
        {
            var siteConfig = TautSiteConfiguration.LoadNew(config, tautSiteName);

            siteConfig.ResolveReverseLinks(config);

            if (siteConfig.ReverseLinks.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Taut site '{tautSiteName}' is linked by others (e.g., '{siteConfig.ReverseLinks[0]}')"
                );
            }

            var tautSitePath = hostRepo.GetTautSitePath(siteConfig.SiteName);

            GitRepoHelpers.DeleteGitDir(tautSitePath);
        }

        var launchDir = actionHelpers.LaunchDirectory ?? Directory.GetCurrentDirectory();

        gitCli.Execute(
            "-C",
            launchDir,
            "config",
            "remove-section",
            "--local",
            $@"{TautSiteConfiguration.SectionName}.{tautSiteName}"
        );
    }

    internal void Reveal(ParseResult parseResult)
    {
        var path = parseResult.GetRequiredValue(ProgramCommandLine.PathArgument);

        var hostRepo = actionHelpers.LocateHostRepo();

        var result = actionHelpers.ResolveTargetOption(parseResult, hostRepo, followHead: true);

        tautSetup.GearUpExisting(hostRepo, null, result.SiteName);

        if (File.Exists(path))
        {
            if (hostRepo.TryGetRelativePathToWorkDir(path, out var relPath) == false)
            {
                Console.Error.WriteLine($"'{path}' not inside the workdir");

                throw new OperationCanceledException();
            }

            var fileStatus = hostRepo.GetFileStatus(relPath);
            if (fileStatus != Lg2StatusFlags.LG2_STATUS_CURRENT)
            {
                Console.Error.WriteLine(
                    $"The file either contians outstanding change, or is not managed by git."
                );

                throw new OperationCanceledException();
            }

            var index = hostRepo.GetIndex();
            var entry = index.GetEntry(relPath, 0);

            var oidEntryHex = entry.GetOidPlainRef().GetOidHexDigits();
            Console.WriteLine($"Source object id: {oidEntryHex}");

            Lg2Oid targetOid = new();
            tautMapping.GetTautened(entry, ref targetOid);

            Console.WriteLine($"Target object id: {targetOid.GetOidHexDigits()}");

            using var tautOdb = tautSetup.TautRepo.GetOdb();

            using (var stream = tautOdb.ReadAsStream(targetOid))
            {
                var decryptor = cipher.CreateDecryptor(stream);

                decryptor.ProduceOutput(Stream.Null);

                var isCompressed = decryptor.IsCompressed;
                var outputLength = decryptor.GetOutputLength();
                var extraInfo = decryptor.GetExtraPayload();
                var extraInfoText = Convert.ToHexStringLower(extraInfo);

                Console.WriteLine($"Compressed: {isCompressed}");
                Console.WriteLine($"Output length: {outputLength}");
                if (string.IsNullOrEmpty(extraInfoText))
                {
                    Console.WriteLine($"Extra payload: <Empty>");
                }
                else
                {
                    Console.WriteLine($"Extra payload: {extraInfoText}");
                }
            }
        }
        else
        {
            Console.Error.WriteLine($"Not a valid path: '{path}'");
        }
    }

    internal void Rescan(ParseResult parseResult)
    {
        var hostRepo = actionHelpers.LocateHostRepo();
        var tautSiteName = actionHelpers.ResolveTargetOption(
            parseResult,
            hostRepo,
            followHead: false
        );

        throw new NotImplementedException();
        // using (var tautRepo = LocateTautRepo())
        // {
        //     // tautManager.Init(tautRepo.GetPath(), null);
        // }

        // tautManager.RebuildKvStore();
    }
}

internal class ProgramCommandLine(IHost host)
{
    internal static Argument<string> RemoteNameArgument = new("remote-name")
    {
        Description = "The name of the remote passed by Git",
    };

    internal static Argument<string> RemoteAddressArgument = new("remote-address")
    {
        Description = "The address of the remote passed by Git",
    };

    internal static Option<string> SiteTargetOption = new("--target")
    {
        Description =
            "Specify a taut site either by its name or by the name of an associated remote",
        Recursive = true,
    };

    internal static Argument<string> CommandNameArgument = new("command-name")
    {
        Description = "Name of the command to execute",
        Arity = ArgumentArity.ExactlyOne,
    };

    internal static Argument<string[]> CommandArgsArgument = new("command-args")
    {
        Description = "Extra arguments for the command",
    };

    internal static Option<bool> LinkExistingOption = new("--link-existing")
    {
        Description =
            $"Whether to setup a a link to exisitng taut site specified by {SiteTargetOption.Name}",
    };

    internal static Argument<string> PathArgument = new("path")
    {
        Description = "Specify the path",
    };

    internal ParseResult ParseForGitTaut(
        string[] args,
        ParserConfiguration? parserConfiguration = null
    )
    {
        var rootCommand = BuildGitTautCommands();
        var parseResult = rootCommand.Parse(args, parserConfiguration);

        return parseResult;
    }

    internal ParseResult ParseForGitRemoteTaut(
        string[] args,
        ParserConfiguration? parserConfiguration = null
    )
    {
        var rootCommand = BuildGitRemoteTautCommands();
        var parseResult = rootCommand.Parse(args, parserConfiguration);

        return parseResult;
    }

    RootCommand BuildGitTautCommands()
    {
        RootCommand rootCommand = new($"{AppInfo.GitTautCommandName} command line")
        {
            SiteTargetOption,
        };

        rootCommand.Subcommands.Add(CreateCommandSiteRun());
        rootCommand.Subcommands.Add(CreateCommandSiteAdd());
        rootCommand.Subcommands.Add(CreateCommandSiteList());
        rootCommand.Subcommands.Add(CreateCommandSiteRemove());
        rootCommand.Subcommands.Add(CreateCommandSiteReveal());

#if DEBUG
        rootCommand.Subcommands.Add(CreateCommandServeHttp());
        rootCommand.Subcommands.Add(CreateCommandSshBypass());
#endif

        CustomizeVersionOption(rootCommand);

        return rootCommand;
    }

    class CustomVersionOptionAction(SynchronousCommandLineAction? defaultAction)
        : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            var rc = defaultAction?.Invoke(parseResult) ?? 0;

            if (AppEnvironment.GetGitTautVersionShowMore())
            {
                var lg2Version = Lg2Global.Version;
                Console.WriteLine("== More ==");
                Console.WriteLine($"Libgit2 version: {lg2Version}");
                Console.WriteLine($"Cipher schema: {nameof(Aes256Cbc1)}");
                Console.WriteLine(
                    $"  Maximum plain text length: {Aes256Cbc1.PLAIN_TEXT_MAX_SIZE} Bytes ({Aes256Cbc1.PLAIN_TEXT_MAX_SIZE / 1024 / 1024} MB)"
                );
            }

            return rc;
        }
    }

    void CustomizeVersionOption(RootCommand rootCommand)
    {
        for (int i = 0; i < rootCommand.Options.Count; i++)
        {
            if (rootCommand.Options[i] is VersionOption defaultVersionOption)
            {
                var defaultAction = defaultVersionOption.Action as SynchronousCommandLineAction;

                defaultVersionOption.Action = new CustomVersionOptionAction(defaultAction);
            }
        }
    }

    RootCommand BuildGitRemoteTautCommands()
    {
        RootCommand rootCommand = new($"{AppInfo.GitRemoteTautCommandName} command line");

        rootCommand.Arguments.Add(RemoteNameArgument);
        rootCommand.Arguments.Add(RemoteAddressArgument);

        rootCommand.SetAction(
            (parseResult, cancellation) =>
            {
                var cmd = host.Services.GetRequiredService<GitRemoteHelper>();

                var remoteName = parseResult.GetRequiredValue(RemoteNameArgument);
                var remoteAddress = parseResult.GetRequiredValue(RemoteAddressArgument);

                var result = cmd.WorkWithGitAsync(remoteName, remoteAddress, cancellation);

                return result;
            }
        );

        return rootCommand;
    }

#if DEBUG
    Command CreateCommandServeHttp()
    {
        var builder = host.Services.GetRequiredService<GitHttpBackendCommandBuilder>();

        return builder.BuildCommand();
    }

    Command CreateCommandSshBypass()
    {
        var builder = host.Services.GetRequiredService<GitSshBypassCommandBuilder>();

        return builder.BuildCommand();
    }
#endif

    Command CreateCommandSiteRun()
    {
        Command command = new("run", "Invoke a git command on the taut site")
        {
            CommandNameArgument,
            CommandArgsArgument,
        };

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(
            (parseResult, cancellation) =>
            {
                return actions.PerformAction(
                    parseResult,
                    cancellation,
                    actions.Run,
                    nameof(actions.Run)
                );
            }
        );

        return command;
    }

    Command CreateCommandSiteAdd()
    {
        Command command = new("add", "Initialize a new taut site for a remote")
        {
            RemoteNameArgument,
            RemoteAddressArgument,
            LinkExistingOption,
        };

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(
            (parseResult, cancellation) =>
            {
                return actions.PerformAction(
                    parseResult,
                    cancellation,
                    actions.Add,
                    nameof(actions.Add)
                );
            }
        );
        return command;
    }

    Command CreateCommandSiteList()
    {
        Command command = new("list", "List about taut sites");

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(
            (parseResult, cancellation) =>
            {
                return actions.PerformAction(
                    parseResult,
                    cancellation,
                    actions.List,
                    nameof(actions.List)
                );
            }
        );

        return command;
    }

    Command CreateCommandSiteRemove()
    {
        Command command = new("remove", "Remove a taut site");

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(
            (parseResult, cancellation) =>
            {
                return actions.PerformAction(
                    parseResult,
                    cancellation,
                    actions.Remove,
                    nameof(actions.Remove)
                );
            }
        );

        return command;
    }

    Command CreateCommandSiteReveal()
    {
        Command command = new("reveal", "Reveal the information about a path in the taut site")
        {
            PathArgument,
        };

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(
            (parseResult, cancellation) =>
            {
                return actions.PerformAction(
                    parseResult,
                    cancellation,
                    actions.Reveal,
                    nameof(actions.Reveal)
                );
            }
        );

        return command;
    }

    Command CreateCommandSiteRescan()
    {
        Command command = new("rescan", "Rescan and rebuild the mapping for the taut site");

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(
            (parseResult, cancellation) =>
            {
                return actions.PerformAction(
                    parseResult,
                    cancellation,
                    actions.Rescan,
                    nameof(actions.Rescan)
                );
            }
        );

        return command;
    }

    internal void SetLg2TraceOutput()
    {
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Lg2Trace>();

        Lg2Trace.SetTraceOutput(
            (message) =>
            {
                logger.ZLogTrace($"{message}");
            }
        );
    }
}
