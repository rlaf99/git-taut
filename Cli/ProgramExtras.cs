using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;

namespace ProgramExtras;

static class HostApplicationBuilderExtensions
{
    internal static void AddGitTautCommandActions(this HostApplicationBuilder hostBuilder)
    {
        hostBuilder.Services.AddSingleton<GitRemoteHelper>();
        hostBuilder.Services.AddSingleton<CampCommandActions>();
        hostBuilder.Services.AddSingleton<OtherCommandActions>();
    }

    internal static void AddGitTautConfiguration(this HostApplicationBuilder hostBuilder)
    {
        var config = hostBuilder.Configuration;

        config.AddEnvironmentVariables();
    }

    internal static void AddGitTautServices(this HostApplicationBuilder hostBuilder)
    {
        var services = hostBuilder.Services;

        services.AddSingleton<GitCli>();
        services.AddSingleton<TautSetup>();
        services.AddSingleton<TautManager>();
        services.AddSingleton<TautMapping>();
        services.AddSingleton<Aes256Cbc1>();
        services.AddSingleton<RecyclableMemoryStreamManager>();
    }

    internal static void AddGitTautLogging(this HostApplicationBuilder hostBuilder)
    {
        var logging = hostBuilder.Logging;
        var config = hostBuilder.Configuration;

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

        if (config.GetGitTautTrace())
        {
            logging.SetMinimumLevel(LogLevel.Trace);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Information);
        }
    }
}

class CampCommandActions(
    // ILogger<CampCommandActions> logger,
    GitCli gitCli,
    TautSetup tautSetup,
    TautManager tautManager,
    TautMapping tautMapping,
    Aes256Cbc1 cipher
)
{
    internal void Run(ParseResult parseResult)
    {
        var cmdName = parseResult.GetRequiredValue(ProgramCommandLine.CommandNameArgument);
        var cmdArgs = parseResult.GetValue(ProgramCommandLine.CommandArgsArgument) ?? [];

        var hostRepo = LocateHostRepo();

        var result = ResolveTargetOption(parseResult, hostRepo, followHead: true);

        tautSetup.GearUpExisting(hostRepo, remoteName: null, result.TautCampName);

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

    internal void Add(ParseResult parseResult)
    {
        var remoteName = parseResult.GetRequiredValue(ProgramCommandLine.RemoteNameArgument);
        var remoteAddress = parseResult.GetRequiredValue(ProgramCommandLine.RemoteAddressArgument);

        var linkExisting = parseResult.GetValue(ProgramCommandLine.LinkExistingOption);

        var hostRepo = LocateHostRepo();

        string? tautCampName = null;
        if (parseResult.GetValue(ProgramCommandLine.CampTargetOption) is not null)
        {
            var result = ResolveTargetOption(parseResult, hostRepo, followHead: false);
            tautCampName = result.TautCampName;
        }

        if (linkExisting && tautCampName is null)
        {
            throw new InvalidOperationException(
                $"{ProgramCommandLine.LinkExistingOption} is used, but no {ProgramCommandLine.CampTargetOption.HelpName} is speficied"
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

        tautSetup.GearUpBrandNew(hostRepo, remoteName, remoteUrl, tautCampName);

        tautManager.RegainHostRefs();

        tautSetup.WrapUpBrandNew();
    }

    internal void List(ParseResult parseResult)
    {
        var hostRepo = LocateHostRepo();

        string? tautCampName = null;

        if (parseResult.GetValue(ProgramCommandLine.CampTargetOption) is not null)
        {
            var result = ResolveTargetOption(parseResult, hostRepo, followHead: false);
            tautCampName = result.TautCampName;
        }

        using (var config = hostRepo.GetConfigSnapshot())
        {
            config.PrintTautCamps(tautCampName);
        }
    }

    internal void Remove(ParseResult parseResult)
    {
        var hostRepo = LocateHostRepo();

        var resolvedTarget = ResolveTargetOption(parseResult, hostRepo, followHead: false);
        var tautCampName = resolvedTarget.TautCampName;

        using (var config = hostRepo.GetConfig())
        {
            if (TautConfig.TryLoadByTautCampName(config, tautCampName, out var tautCfg) == false)
            {
                throw new InvalidOperationException($"Failed to load '{tautCampName}");
            }

            tautCfg.ResolveReverseLinks(config);

            if (tautCfg.ReverseLinks.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Taut camp '{tautCampName}' is linked by others (e.g., '{tautCfg.ReverseLinks[0]})'"
                );
            }

            tautCfg.ResolveRemotes(config);

            if (resolvedTarget.TargetIsRemote)
            {
                tautCfg.RemoveRemoteFromConfig(config, resolvedTarget.RemoteName!);
            }

            // if (resolvedTarget.TargetIsRemote && tautCfg)

            throw new NotImplementedException();

            // remove config
            // remove repo
            // remove remote
        }
    }

    internal void Reveal(ParseResult parseResult)
    {
        var path = parseResult.GetRequiredValue(ProgramCommandLine.PathArgument);

        var hostRepo = LocateHostRepo();

        var result = ResolveTargetOption(parseResult, hostRepo, followHead: true);

        tautSetup.GearUpExisting(hostRepo, null, result.TautCampName);

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
    }

    internal void Rescan(ParseResult parseResult)
    {
        var hostRepo = LocateHostRepo();
        var tautCampName = ResolveTargetOption(parseResult, hostRepo, followHead: false);

        throw new NotImplementedException();
        // using (var tautRepo = LocateTautRepo())
        // {
        //     // tautManager.Init(tautRepo.GetPath(), null);
        // }

        // tautManager.RebuildKvStore();
    }

    record ResolveTargetOptionResult(
        string TautCampName,
        string? RemoteName,
        bool TargetIsRemote,
        bool CampIsFromHead
    );

    ResolveTargetOptionResult ResolveTargetOption(
        ParseResult parseResult,
        Lg2Repository hostRepo,
        bool followHead
    )
    {
        var targetName = parseResult.GetValue(ProgramCommandLine.CampTargetOption);
        if (targetName is not null)
        {
            using var config = hostRepo.GetConfigSnapshot();

            TautConfig? tautConfig;

            if (TautConfig.TryLoadByTautCampName(config, targetName, out tautConfig))
            {
                return new(
                    TautCampName: tautConfig.CampName,
                    RemoteName: null,
                    TargetIsRemote: false,
                    CampIsFromHead: false
                );
            }

            if (TautConfig.TryLoadByRemoteName(config, targetName, out tautConfig))
            {
                return new(
                    TautCampName: tautConfig.CampName,
                    RemoteName: targetName,
                    TargetIsRemote: true,
                    CampIsFromHead: false
                );
            }

            throw new InvalidOperationException(
                $"Invalid value '{targetName}' specified by {ProgramCommandLine.CampTargetOption.Name}"
            );
        }
        else if (followHead)
        {
            if (TryResolveTautCampNameFromHead(hostRepo, out var tautCampName))
            {
                return new(
                    TautCampName: tautCampName,
                    RemoteName: null,
                    TargetIsRemote: false,
                    CampIsFromHead: true
                );
            }

            throw new InvalidOperationException($"Cannot resolve taut camp name specified by HEAD");
        }
        else
        {
            throw new InvalidOperationException(
                $"{ProgramCommandLine.CampTargetOption.Name} is not specified"
            );
        }
    }

    bool TryResolveTautCampNameFromHead(
        Lg2Repository hostRepo,
        [NotNullWhen(true)] out string? tautCampName
    )
    {
        var repoHead = hostRepo.GetHead();
        if (repoHead.IsBranch() == false)
        {
            tautCampName = null;
            return false;
        }

        var headRefName = repoHead.GetName();
        var remoteName = hostRepo.GetBranchUpstreamRemoteName(headRefName);

        using (var config = hostRepo.GetConfigSnapshot())
        {
            if (TautConfig.TryLoadByRemoteName(config, remoteName, out var tautConfig))
            {
                tautCampName = tautConfig.CampName;

                return true;
            }
        }

        tautCampName = null;
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
            Console.Error.WriteLine($"Failed to locate host repo: not inside a git repository");

            throw new OperationCanceledException();
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

        var tautRepoFullPath = GitRepoExtra.GetTautHomePath(hostRepo.GetPath());
        var tautRepoRelPath = Path.GetRelativePath(currentDir, tautRepoFullPath);

        return OpenTautRepo(tautRepoRelPath);
    }
}

class OtherCommandActions()
{
    internal void ShowInternal(ParseResult parseResult)
    {
        var lg2Version = Lg2Global.Version;
        Console.WriteLine($"Libgit2 version: {lg2Version}");
        Console.WriteLine($"Cipher schema: {nameof(Aes256Cbc1)}");
        Console.WriteLine(
            $"  Maximum plain text length: {Aes256Cbc1.PLAIN_TEXT_MAX_SIZE} Bytes ({Aes256Cbc1.PLAIN_TEXT_MAX_SIZE / 1024 / 1024} MB)"
        );
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

    internal static Option<string> CampTargetOption = new("--target")
    {
        Description =
            "Specify a taut camp either by its name or by the name of an associated remote",
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
            $"Whether to setup a a link to exisitng taut camp specified by {CampTargetOption.Name}",
    };

    internal static Argument<string> PathArgument = new("path")
    {
        Description = "Specify the path",
    };

    internal int Parse(string[] args)
    {
        var rootCommand = BuildCommands();
        var parseResult = rootCommand.Parse(args);

        return parseResult.Invoke();
    }

    RootCommand BuildCommands()
    {
        RootCommand rootCommand = new("git-taut command lines");

        var campCommand = CreateCommandCamp();

        campCommand.Subcommands.Add(CreateCommandCampRun());
        campCommand.Subcommands.Add(CreateCommandCampAdd());
        campCommand.Subcommands.Add(CreateCommandCampList());
        campCommand.Subcommands.Add(CreateCommandCampRemove());
        campCommand.Subcommands.Add(CreateCommandCampReveal());
        campCommand.Subcommands.Add(CreateCommandCampRescan());

        rootCommand.Subcommands.Add(campCommand);
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

    Command CreateCommandCamp()
    {
        Command command = new("camp", "Taut camp related commands") { CampTargetOption };

        return command;
    }

    Command CreateCommandCampRun()
    {
        Command command = new("run", "Invoke a git command on the taut camp")
        {
            CommandNameArgument,
            CommandArgsArgument,
        };

        var actions = host.Services.GetRequiredService<CampCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Run(parseResult);
        });

        return command;
    }

    Command CreateCommandCampAdd()
    {
        Command command = new("add", "Initialize a new taut camp for a remote")
        {
            RemoteNameArgument,
            RemoteAddressArgument,
            LinkExistingOption,
        };

        var actions = host.Services.GetRequiredService<CampCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Add(parseResult);
        });

        return command;
    }

    Command CreateCommandCampList()
    {
        Command command = new("list", "List about taut camps");

        var actions = host.Services.GetRequiredService<CampCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.List(parseResult);
        });

        return command;
    }

    Command CreateCommandCampRemove()
    {
        Command command = new("remove", "Remove a taut camp");

        var actions = host.Services.GetRequiredService<CampCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Remove(parseResult);
        });

        return command;
    }

    Command CreateCommandCampReveal()
    {
        Command command = new("reveal", "Reveal the information about a path in the taut camp")
        {
            PathArgument,
        };

        var actions = host.Services.GetRequiredService<CampCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Reveal(parseResult);
        });

        return command;
    }

    Command CreateCommandCampRescan()
    {
        Command command = new("rescan", "Rescan and rebuild the mapping for the taut camp");

        var actions = host.Services.GetRequiredService<CampCommandActions>();

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
