using System.CommandLine;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;

static class HostApplicationBuilderExtensions
{
    internal static void AddCommandActions(this HostApplicationBuilder hostBuilder)
    {
        hostBuilder.Services.AddSingleton<GitRemoteHelper>();
        hostBuilder.Services.AddSingleton<BaseCommandActions>();
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

class BaseCommandActions(
    GitCli gitCli,
    TautSetup tautSetup,
    TautManager tautManager,
    TautMapping tautMapping,
    Aes256Cbc1 cipher
)
{
    public void Run(ParseResult parseResult)
    {
        var remoteName = parseResult.GetRequiredValue(ProgramCommandLine.BaseRemoteOption);

        var cmdName = parseResult.GetRequiredValue(ProgramCommandLine.CommandNameArgument);
        var cmdArgs = parseResult.GetValue(ProgramCommandLine.CommandArgsArgument) ?? [];

        var hostRepo = LocateHostRepo();

        using var config = hostRepo.GetConfigSnapshot();
        var tautBaseName = config.FindTautBaseName(remoteName);
        tautSetup.GearUpExisting(hostRepo, remoteName, tautBaseName);

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

    public void Add(ParseResult parseResult)
    {
        var remoteName = parseResult.GetRequiredValue(ProgramCommandLine.RemoteNameArgument);
        var remoteAddress = parseResult.GetRequiredValue(ProgramCommandLine.RemoteAddressArgument);
        var linkTo = parseResult.GetValue(ProgramCommandLine.BaseRemoteOption);

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

        string? tautBaseNameToLink = null;
        if (linkTo is not null)
        {
            if (
                TautConfig.TryLoadByTautBaseName(hostRepo, linkTo, out TautConfig? tautConfig)
                || TautConfig.TryLoadByRemoteName(hostRepo, linkTo, out tautConfig)
            )
            {
                tautBaseNameToLink = tautConfig.TautBaseName;
            }
            else
            {
                Console.Error.WriteLine(
                    $"Cannot load {nameof(TautConfig)} with '{linkTo}' either by taut repository name or by remote name"
                );

                throw new OperationCanceledException();
            }
        }

        tautSetup.GearUpBrandNew(hostRepo, remoteName, remoteUrl, tautBaseNameToLink);

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

    public void Reveal(ParseResult parseResult)
    {
        var remoteName = parseResult.GetRequiredValue(ProgramCommandLine.BaseRemoteOption);
        var path = parseResult.GetRequiredValue(ProgramCommandLine.PathArgument);

        var hostRepo = LocateHostRepo();

        using var config = hostRepo.GetConfigSnapshot();
        var tautBaseName = config.FindTautBaseName(remoteName);

        tautSetup.GearUpExisting(hostRepo, remoteName, tautBaseName);

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
        throw new NotImplementedException();
        // using (var tautRepo = LocateTautRepo())
        // {
        //     // tautManager.Init(tautRepo.GetPath(), null);
        // }

        // tautManager.RebuildKvStore();
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
    internal int Parse(string[] args)
    {
        var rootCommand = BuildCommands();
        var parseResult = rootCommand.Parse(args);

        return parseResult.Invoke();
    }

    RootCommand BuildCommands()
    {
        RootCommand rootCommand = new("git-taut command lines");

        var baseCommand = CreateCommandBase();

        baseCommand.Subcommands.Add(CreateCommandBaseRun());
        baseCommand.Subcommands.Add(CreateCommandBaseAdd());
        baseCommand.Subcommands.Add(CreateCommandBaseList());
        baseCommand.Subcommands.Add(CreateCommandBaseReveal());
        baseCommand.Subcommands.Add(CreateCommandBaseRescan());

        rootCommand.Subcommands.Add(baseCommand);
        rootCommand.Subcommands.Add(CreateCommandRemoteHelper());
        rootCommand.Subcommands.Add(CreateCommandShowInternal());

        return rootCommand;
    }

    internal static Argument<string> RemoteNameArgument = new("remote-name")
    {
        Description = "The name of the remote passed by Git",
    };

    internal static Argument<string> RemoteAddressArgument = new("remote-address")
    {
        Description = "The address of the remote passed by Git",
    };

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

    internal static Option<string> BaseRemoteOption = new("--remote")
    {
        Description = "Name of a remote associated with the taut base",
    };

    internal static Option<string> BaseSelectOption = new("--select")
    {
        Description = "Name of the taut base to use",
    };

    Command CreateCommandBase()
    {
        Command command = new("base", "Taut base related commands")
        {
            BaseRemoteOption,
            BaseSelectOption,
        };

        return command;
    }

    internal static Argument<string> CommandNameArgument = new("command-name")
    {
        Description = "Name of the command to execute",
        Arity = ArgumentArity.ExactlyOne,
    };

    internal static Argument<string[]> CommandArgsArgument = new("command-args")
    {
        Description = "Extra arguments for the command",
    };

    Command CreateCommandBaseRun()
    {
        Command command = new("run", "Invoke git command on the taut base")
        {
            CommandNameArgument,
            CommandArgsArgument,
        };

        var actions = host.Services.GetRequiredService<BaseCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Run(parseResult);
        });

        return command;
    }

    internal static Option<string> LinkToOption = new("--link-to")
    {
        Description = "Name of an existing taut base to link to",
    };

    Command CreateCommandBaseAdd()
    {
        Command command = new("add", "Initialize a new taut base for a remote")
        {
            RemoteNameArgument,
            RemoteAddressArgument,
            LinkToOption,
        };

        var actions = host.Services.GetRequiredService<BaseCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Add(parseResult);
        });

        return command;
    }

    Command CreateCommandBaseList()
    {
        Command command = new("list", "List existing taut bases");

        var actions = host.Services.GetRequiredService<BaseCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.List(parseResult);
        });

        return command;
    }

    internal static Argument<string> PathArgument = new("path")
    {
        Description = "Specify the path",
    };

    Command CreateCommandBaseReveal()
    {
        Command command = new("reveal", "Reveal the information about a path in the taut base")
        {
            PathArgument,
        };

        var actions = host.Services.GetRequiredService<BaseCommandActions>();

        command.SetAction(parseResult =>
        {
            actions.Reveal(parseResult);
        });

        return command;
    }

    Command CreateCommandBaseRescan()
    {
        Command command = new("rescan", "Rescan and rebuild the mapping for the taut base");

        var actions = host.Services.GetRequiredService<BaseCommandActions>();

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
