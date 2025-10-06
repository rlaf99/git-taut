using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;

namespace ProgramSupport;

static class HostApplicationBuilderExtensions
{
    internal static void AddCommandActions(this HostApplicationBuilder hostBuilder)
    {
        hostBuilder.Services.AddSingleton<GitRemoteHelper>();
        hostBuilder.Services.AddSingleton<CampCommandActions>();
        hostBuilder.Services.AddSingleton<OtherCommandActions>();
    }

    internal static void AddConfiguration(this HostApplicationBuilder hostBuilder)
    {
        var config = hostBuilder.Configuration;

        config.AddEnvironmentVariables();
    }

    internal static void AddServices(this HostApplicationBuilder hostBuilder)
    {
        var services = hostBuilder.Services;

        services.AddSingleton<GitCli>();
        services.AddSingleton<TautSetup>();
        services.AddSingleton<TautManager>();
        services.AddSingleton<TautMapping>();
        services.AddSingleton<Aes256Cbc1>();
        services.AddSingleton<RecyclableMemoryStreamManager>();
    }

    internal static void AddLogging(this HostApplicationBuilder hostBuilder)
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

        var tautCampName = ResolveTargetOption(parseResult, hostRepo, followHead: true);

        tautSetup.GearUpExisting(hostRepo, remoteName: null, tautCampName);

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
        var linkTo = parseResult.GetValue(ProgramCommandLine.CampTargetOption);

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

        var hostRepo = LocateHostRepo();

        if (hostRepo.TryLookupRemote(remoteName, out _))
        {
            Console.Error.WriteLine($"Remote '{remoteName}' already exists in the host repo.");
            throw new OperationCanceledException();
        }

        string? tautCampNameToLink = null;
        if (linkTo is not null)
        {
            using var config = hostRepo.GetConfigSnapshot();
            if (
                TautConfig.TryLoadByTautCampName(config, linkTo, out TautConfig? tautConfig)
                || TautConfig.TryLoadByRemoteName(config, linkTo, out tautConfig)
            )
            {
                tautCampNameToLink = tautConfig.CampName;
            }
            else
            {
                Console.Error.WriteLine(
                    $"Cannot load {nameof(TautConfig)} with '{linkTo}' either by taut repository name or by remote name"
                );

                throw new OperationCanceledException();
            }
        }

        tautSetup.GearUpBrandNew(hostRepo, remoteName, remoteUrl, tautCampNameToLink);

        tautManager.RegainHostRefs();

        tautSetup.WrapUpBrandNew();
    }

    internal void List(ParseResult parseResult)
    {
        var hostRepo = LocateHostRepo();

        using (var config = hostRepo.GetConfigSnapshot())
        {
            config.PrintAllTaut();
        }
    }

    internal void Remove(ParseResult parseResult)
    {
        throw new NotImplementedException();
    }

    internal void Reveal(ParseResult parseResult)
    {
        var path = parseResult.GetRequiredValue(ProgramCommandLine.PathArgument);

        var hostRepo = LocateHostRepo();

        var tautCampName = ResolveTargetOption(parseResult, hostRepo, followHead: true);

        tautSetup.GearUpExisting(hostRepo, null, tautCampName);

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

    string ResolveTargetOption(ParseResult parseResult, Lg2Repository hostRepo, bool followHead)
    {
        var targetName = parseResult.GetValue(ProgramCommandLine.CampTargetOption);
        if (targetName is not null)
        {
            if (TryResolveTargetOptionFromConfig(hostRepo, targetName, out var tautCampName))
            {
                return tautCampName;
            }
            else
            {
                throw new OperationCanceledException(
                    $"Invalid value '{targetName}' specified by {ProgramCommandLine.CampTargetOption.Name}"
                );
            }
        }
        else if (followHead)
        {
            if (TryResolveTautCampNameFromHead(hostRepo, out var tautCampName))
            {
                return tautCampName;
            }
            else
            {
                throw new OperationCanceledException(
                    $"Cannot resolve taut camp name specified by HEAD"
                );
            }
        }
        else
        {
            throw new OperationCanceledException(
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

    bool TryResolveTargetOptionFromConfig(
        Lg2Repository hostRepo,
        string targetName,
        [NotNullWhen(true)] out string? tautCampName
    )
    {
        using var config = hostRepo.GetConfigSnapshot();

        if (
            TautConfig.TryLoadByTautCampName(config, targetName, out var tautConfig)
            || TautConfig.TryLoadByRemoteName(config, targetName, out tautConfig)
        )
        {
            tautCampName = tautConfig.CampName;

            return true;
        }
        else
        {
            tautCampName = null;

            return false;
        }
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
