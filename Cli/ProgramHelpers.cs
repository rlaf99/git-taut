using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;

namespace ProgramHelpers;

static class HostApplicationBuilderExtensions
{
    internal static void AddGitTautReleated(this HostApplicationBuilder builder)
    {
        builder.AddGitTautConfiguration();
        builder.AddGitTautServices();
        builder.AddGitTautCommandActions();
        builder.AddGitTautLogging();
    }

    internal static void AddGitTautCommandActions(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<GitRemoteHelper>();
        builder.Services.AddSingleton<SiteCommandActions>();
        builder.Services.AddSingleton<OtherCommandActions>();
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
        services.AddSingleton<Aes256Cbc1>();
        services.AddSingleton<RecyclableMemoryStreamManager>();
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
                            ProgramInfo.CommandName,
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

class SiteCommandActions(
    ILogger<SiteCommandActions> logger,
    GitCli gitCli,
    TautSetup tautSetup,
    TautManager tautManager,
    TautMapping tautMapping,
    Aes256Cbc1 cipher
)
{
    internal void Run(ParseResult parseResult)
    {
        logger.ZLogTrace($"Start invoking action {nameof(Run)}");

        var cmdName = parseResult.GetRequiredValue(ProgramCommandLine.CommandNameArgument);
        var cmdArgs = parseResult.GetValue(ProgramCommandLine.CommandArgsArgument) ?? [];

        var hostRepo = LocateHostRepo();

        var result = ResolveTargetOption(parseResult, hostRepo, followHead: true);

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

        logger.ZLogTrace($"Done invoking action {nameof(Run)}");
    }

    internal Task<int> AddAsync(ParseResult parseResult, CancellationToken cancelToken)
    {
        logger.ZLogTrace($"Start invoking action {nameof(Add)}");

        try
        {
            Add(parseResult);
        }
        catch (InvalidOperationException ex)
        {
            var error = parseResult.InvocationConfiguration.Error;
            error.WriteLine(ex.Message);

            return Task.FromResult(1);
        }

        logger.ZLogTrace($"Done invoking action {nameof(Add)}");

        return Task.FromResult(0);
    }

    internal void Add(ParseResult parseResult)
    {
        var remoteName = parseResult.GetRequiredValue(ProgramCommandLine.RemoteNameArgument);
        var remoteAddress = parseResult.GetRequiredValue(ProgramCommandLine.RemoteAddressArgument);

        var linkExisting = parseResult.GetValue(ProgramCommandLine.LinkExistingOption);

        var hostRepo = LocateHostRepo();

        string? tautSiteName = null;
        if (parseResult.GetValue(ProgramCommandLine.SiteTargetOption) is not null)
        {
            var result = ResolveTargetOption(parseResult, hostRepo, followHead: false);
            tautSiteName = result.SiteName;
        }

        if (linkExisting && tautSiteName is null)
        {
            throw new InvalidOperationException(
                $"No {ProgramCommandLine.SiteTargetOption.Name} is speficied when {ProgramCommandLine.LinkExistingOption.Name} is used"
            );
        }

        string remoteUrl;

        if (Directory.Exists(remoteAddress))
        {
            try
            {
                using var repo = Lg2Repository.New(
                    remoteAddress,
                    Lg2RepositoryOpenFlags.LG2_REPOSITORY_OPEN_NO_SEARCH
                );

                remoteUrl = repo.GetPath();
            }
            catch (Lg2Exception)
            {
                Console.Error.WriteLine($"Failed to open '{remoteAddress}' as a local git repo");

                throw new OperationCanceledException();
            }
        }
        else if (remoteAddress.Contains(':'))
        {
            remoteUrl = remoteAddress;
        }
        else
        {
            Console.Error.WriteLine($"Unrecoginized repo path '{remoteAddress}'");

            throw new OperationCanceledException();
        }

        if (hostRepo.TryLookupRemote(remoteName, out _))
        {
            Console.Error.WriteLine($"Remote '{remoteName}' already exists in the host repo.");
            throw new OperationCanceledException();
        }

        tautSetup.GearUpBrandNew(hostRepo, remoteName, remoteUrl, tautSiteName);

        using (tautSetup)
        {
            tautManager.RegainHostRefs();

            tautSetup.WrapUpBrandNew();
        }
    }

    internal Task<int> ListAsync(ParseResult parseResult, CancellationToken cancelToken)
    {
        logger.ZLogTrace($"Start invoking action {nameof(List)}");

        try
        {
            List(parseResult);
        }
        catch (InvalidOperationException ex)
        {
            var error = parseResult.InvocationConfiguration.Error;
            error.WriteLine(ex.Message);

            return Task.FromResult(1);
        }

        logger.ZLogTrace($"Done invoking action {nameof(List)}");

        return Task.FromResult(0);
    }

    internal void List(ParseResult parseResult)
    {
        var hostRepo = LocateHostRepo();

        string? tautSiteName = null;

        if (parseResult.GetValue(ProgramCommandLine.SiteTargetOption) is not null)
        {
            var result = ResolveTargetOption(parseResult, hostRepo, followHead: false);
            tautSiteName = result.SiteName;
        }

        using (var config = hostRepo.GetConfigSnapshot())
        {
            TautSiteConfig.PrintSites(config, tautSiteName);
        }
    }

    internal void Remove(ParseResult parseResult)
    {
        logger.ZLogTrace($"Start invoking action {nameof(Remove)}");

        var hostRepo = LocateHostRepo();

        var resolvedTarget = ResolveTargetOption(parseResult, hostRepo, followHead: false);
        var tautSiteName = resolvedTarget.SiteName;

        using (var config = hostRepo.GetConfig())
        {
            if (TautSiteConfig.TryLoadBySiteName(config, tautSiteName, out var tautCfg) == false)
            {
                throw new InvalidOperationException($"Failed to load '{tautSiteName}");
            }

            tautCfg.ResolveReverseLinks(config);

            if (tautCfg.ReverseLinks.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Taut site '{tautSiteName}' is linked by others (e.g., '{tautCfg.ReverseLinks[0]})'"
                );
            }

            tautCfg.ResolveRemotes(config);

            if (resolvedTarget.TargetIsRemote)
            {
                tautCfg.RemoveRemoteFromConfig(config, resolvedTarget.RemoteName!);
            }

            if (tautCfg.Remotes.Count != 0)
            {
                throw new InvalidOperationException(
                    $"There are other remotes using taut site '{tautCfg.SiteName}'"
                );
            }

            tautCfg.RemoveAllFromConfig(config);

            var tautSitePath = hostRepo.GetTautSitePath(tautCfg.SiteName);

            GitRepoHelpers.DeleteGitDir(tautSitePath);
        }

        logger.ZLogTrace($"Done invoking action {nameof(Remove)}");
    }

    internal void Reveal(ParseResult parseResult)
    {
        logger.ZLogTrace($"Start invoking action {nameof(Reveal)}");

        var path = parseResult.GetRequiredValue(ProgramCommandLine.PathArgument);

        var hostRepo = LocateHostRepo();

        var result = ResolveTargetOption(parseResult, hostRepo, followHead: true);

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

            Console.WriteLine($"Target object id: {targetOid.ToHexDigits()}");

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

        logger.ZLogTrace($"Done invoking action {nameof(Reveal)}");
    }

    internal void Rescan(ParseResult parseResult)
    {
        logger.ZLogTrace($"Start invoking action {nameof(Rescan)}");

        var hostRepo = LocateHostRepo();
        var tautSiteName = ResolveTargetOption(parseResult, hostRepo, followHead: false);

        throw new NotImplementedException();
        // using (var tautRepo = LocateTautRepo())
        // {
        //     // tautManager.Init(tautRepo.GetPath(), null);
        // }

        // tautManager.RebuildKvStore();

        logger.ZLogTrace($"Done invoking action {nameof(Rescan)}");
    }

    record ResolveTargetOptionResult(
        string SiteName,
        string? RemoteName,
        bool TargetIsRemote,
        bool SiteIsFromHead
    );

    ResolveTargetOptionResult ResolveTargetOption(
        ParseResult parseResult,
        Lg2Repository hostRepo,
        bool followHead
    )
    {
        var targetName = parseResult.GetValue(ProgramCommandLine.SiteTargetOption);
        if (targetName is not null)
        {
            using var config = hostRepo.GetConfigSnapshot();

            TautSiteConfig? tautConfig;

            if (TautSiteConfig.TryLoadBySiteName(config, targetName, out tautConfig))
            {
                return new(
                    SiteName: tautConfig.SiteName,
                    RemoteName: null,
                    TargetIsRemote: false,
                    SiteIsFromHead: false
                );
            }

            if (TautSiteConfig.TryLoadByRemoteName(config, targetName, out tautConfig))
            {
                return new(
                    SiteName: tautConfig.SiteName,
                    RemoteName: targetName,
                    TargetIsRemote: true,
                    SiteIsFromHead: false
                );
            }

            throw new InvalidOperationException(
                $"The value '{targetName}' specified by {ProgramCommandLine.SiteTargetOption.Name} is invalid"
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

            throw new InvalidOperationException($"Cannot resolve taut site name specified by HEAD");
        }
        else
        {
            throw new InvalidOperationException(
                $"{ProgramCommandLine.SiteTargetOption.Name} is not specified"
            );
        }
    }

    bool TryResolveTautSiteNameFromHead(
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

        using (var config = hostRepo.GetConfigSnapshot())
        {
            if (TautSiteConfig.TryLoadByRemoteName(config, remoteName, out var tautConfig))
            {
                tautSiteName = tautConfig.SiteName;

                return true;
            }
        }

        tautSiteName = null;
        return false;
    }

    Lg2Repository LocateHostRepo()
    {
        if (KnownEnvironVars.TryGetGitDir(out var gitDir))
        {
            return Lg2Repository.New(gitDir);
        }

        var currentDir = Directory.GetCurrentDirectory();
        if (Lg2Repository.TryDiscover(currentDir, out var hostRepo) == false)
        {
            throw new InvalidOperationException($"Not inside a git repository");
        }

        return hostRepo;
    }

    Lg2Repository OpenTautRepo(string repoPath)
    {
        try
        {
            var tautRepo = Lg2Repository.New(repoPath);

            return tautRepo;
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"Cannot open taut repo at path '{repoPath}'");

            throw new OperationCanceledException();
        }
    }

    Lg2Repository LocateTautRepo()
    {
        var currentDir = Directory.GetCurrentDirectory();

        var hostRepo = LocateHostRepo();

        var tautRepoFullPath = GitRepoHelpers.GetTautHomePath(hostRepo.GetPath());
        var tautRepoRelPath = Path.GetRelativePath(currentDir, tautRepoFullPath);

        return OpenTautRepo(tautRepoRelPath);
    }
}

class OtherCommandActions(ILogger<OtherCommandActions> logger)
{
    internal void ShowInternal(ParseResult parseResult)
    {
        logger.ZLogTrace($"Start invoking action {nameof(ShowInternal)}");

        var lg2Version = Lg2Global.Version;
        Console.WriteLine($"Libgit2 version: {lg2Version}");
        Console.WriteLine($"Cipher schema: {nameof(Aes256Cbc1)}");
        Console.WriteLine(
            $"  Maximum plain text length: {Aes256Cbc1.PLAIN_TEXT_MAX_SIZE} Bytes ({Aes256Cbc1.PLAIN_TEXT_MAX_SIZE / 1024 / 1024} MB)"
        );

        logger.ZLogTrace($"Done invoking action {nameof(ShowInternal)}");
    }
}

class ProgramCommandLine(IHost host)
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

    internal ParseResult Parse(string[] args, ParserConfiguration? parserConfiguration = null)
    {
        var rootCommand = BuildCommands();
        var parseResult = rootCommand.Parse(args, parserConfiguration);

        return parseResult;
    }

    RootCommand BuildCommands()
    {
        RootCommand rootCommand = new("git-taut command lines");

        var siteCommand = CreateCommandSite();

        siteCommand.Subcommands.Add(CreateCommandSiteRun());
        siteCommand.Subcommands.Add(CreateCommandSiteAdd());
        siteCommand.Subcommands.Add(CreateCommandSiteList());
        siteCommand.Subcommands.Add(CreateCommandSiteRemove());
        siteCommand.Subcommands.Add(CreateCommandSiteReveal());
        siteCommand.Subcommands.Add(CreateCommandSiteRescan());

        rootCommand.Subcommands.Add(siteCommand);
        rootCommand.Subcommands.Add(CreateCommandRemoteHelper());
        rootCommand.Subcommands.Add(CreateCommandShowInternal());

        return rootCommand;
    }

    Command CreateCommandRemoteHelper()
    {
        Command command = new("remote-helper", "Interact with Git as a remote helper.");

        command.Arguments.Add(RemoteNameArgument);
        command.Arguments.Add(RemoteAddressArgument);

        command.SetAction(
            (parseResult, cancelToken) =>
            {
                var cmd = host.Services.GetRequiredService<GitRemoteHelper>();

                var remoteName = parseResult.GetRequiredValue(RemoteNameArgument);
                var remoteAddress = parseResult.GetRequiredValue(RemoteAddressArgument);

                var result = cmd.WorkWithGitAsync(remoteName, remoteAddress, cancelToken);

                return result;
            }
        );

        return command;
    }

    Command CreateCommandShowInternal()
    {
        Command command = new("show-internal", "Display internal information");

        command.SetAction(parseResult =>
        {
            var commands = host.Services.GetRequiredService<OtherCommandActions>();

            commands.ShowInternal(parseResult);
        });

        return command;
    }

    Command CreateCommandSite()
    {
        Command command = new("site", "Taut site related commands") { SiteTargetOption };

        return command;
    }

    Command CreateCommandSiteRun()
    {
        Command command = new("run", "Invoke a git command on the taut site")
        {
            CommandNameArgument,
            CommandArgsArgument,
        };

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Run(parseResult);
        });

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

        command.SetAction(actions.AddAsync);

        return command;
    }

    Command CreateCommandSiteList()
    {
        Command command = new("list", "List about taut sites");

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(actions.ListAsync);

        return command;
    }

    Command CreateCommandSiteRemove()
    {
        Command command = new("remove", "Remove a taut site");

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Remove(parseResult);
        });

        return command;
    }

    Command CreateCommandSiteReveal()
    {
        Command command = new("reveal", "Reveal the information about a path in the taut site")
        {
            PathArgument,
        };

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Reveal(parseResult);
        });

        return command;
    }

    Command CreateCommandSiteRescan()
    {
        Command command = new("rescan", "Rescan and rebuild the mapping for the taut site");

        var actions = host.Services.GetRequiredService<SiteCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Rescan(parseResult);
        });

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
