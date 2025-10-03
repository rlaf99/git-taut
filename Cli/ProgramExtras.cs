using ConsoleAppFramework;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using ZLogger;

internal class ExtraCommands
{
    /// <summary>Print internal information.</summary>
    [Command("--internal-info")]
    public void InternalInfo()
    {
        var lg2Version = Lg2Global.Version;
        ConsoleApp.Log($"Libgit2 version: {lg2Version}");
        ConsoleApp.Log($"Cipher schema: {nameof(Aes256Cbc1)}");
        ConsoleApp.Log(
            $"  Maximum plain text length: {Aes256Cbc1.PLAIN_TEXT_MAX_SIZE} Bytes ({Aes256Cbc1.PLAIN_TEXT_MAX_SIZE / 1024 / 1024} MB)"
        );
    }

    /// <summary>
    ///  Invoke git commands on a taut repository.
    /// </summary>
    [Command("--git")]
    public void Git(
        [FromServices] GitCli gitCli,
        [FromServices] TautSetup tautSetup,
        [Argument] string[] args,
        string remoteName = "origin"
    )
    {
        var hostRepo = LocateHostRepo();

        using var config = hostRepo.GetConfigSnapshot();
        var tautRepoName = config.FindTautRepoName(remoteName);
        tautSetup.GearUpExisting(hostRepo, remoteName, tautRepoName);

        using (tautSetup)
        {
            string[] gitArgs = ["--git-dir", tautSetup.TautRepo.GetPath(), .. args];
            try
            {
                gitCli.Run(gitArgs);
            }
            catch (Exception)
            {
                ConsoleApp.LogError($"Failed to run `{string.Join(" ", gitArgs)}`");
            }
        }
    }

    [Command("--list")]
    public void List()
    {
        var hostRepo = LocateHostRepo();

        using (var config = hostRepo.GetConfigSnapshot())
        {
            config.PrintAllTaut();
        }
    }

    /// <summary>
    /// Initialize a new remote for taut.
    /// </summary>
    /// <param name="remoteAddress">The address of the new remote.</param>
    /// <param name="remoteName">The name of the new remote.</param>
    /// <param name="linkTo">Either the name of a taut, or the name of a remote that resolves to a taut.
    ///  The corresponding taut's credential and repository are shared with the new remote.</param>
    [Command("--add")]
    public void Add(
        [FromServices] GitCli gitCli,
        [FromServices] TautSetup tautSetup,
        [FromServices] TautManager tautManager,
        [Argument] string remoteAddress,
        string remoteName,
        string? linkTo = null
    )
    {
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
                ConsoleApp.LogError($"Failed to open '{remoteAddress}' as a local git repo");

                throw new OperationCanceledException();
            }
        }
        else if (remoteAddress.Contains(':'))
        {
            remoteUrl = remoteAddress;
        }
        else
        {
            ConsoleApp.LogError($"Unrecoginized repo path '{remoteAddress}'");

            throw new OperationCanceledException();
        }

        var hostRepo = LocateHostRepo();

        if (hostRepo.TryLookupRemote(remoteName, out _))
        {
            ConsoleApp.LogError($"Remote '{remoteName}' already exists in the host repo.");
            throw new OperationCanceledException();
        }

        string? tautRepoNameToLink = null;
        if (linkTo is not null)
        {
            if (
                TautConfig.TryLoadByTautRepoName(hostRepo, linkTo, out TautConfig? tautConfig)
                || TautConfig.TryLoadByRemoteName(hostRepo, linkTo, out tautConfig)
            )
            {
                tautRepoNameToLink = tautConfig.TautRepoName;
            }
            else
            {
                ConsoleApp.LogError(
                    $"Cannot load {nameof(TautConfig)} with '{linkTo}' either by taut repository name or by remote name"
                );

                throw new OperationCanceledException();
            }
        }

        tautSetup.GearUpBrandNew(hostRepo, remoteName, remoteUrl, tautRepoNameToLink);

        tautManager.RegainHostRefs();

        tautSetup.WrapUpBrandNew();
    }

    /// <summary>
    /// Consult the index and reveal the information about a path in a taut.
    /// </summary>
    ///
    /// <param name="path">The path to reveal.</param>
    /// <param name="remote">The name of a remote that resolves to a taut.</parma>
    [Command("--reveal")]
    public void Reveal(
        [FromServices] TautSetup tautSetup,
        [FromServices] TautManager tautManager,
        [FromServices] TautMapping tautMapping,
        [FromServices] Aes256Cbc1 cipher,
        [Argument] string path,
        string remote = "origin"
    )
    {
        var hostRepo = LocateHostRepo();

        using var config = hostRepo.GetConfigSnapshot();
        var tautRepoName = config.FindTautRepoName(remote);

        tautSetup.GearUpExisting(hostRepo, remote, tautRepoName);

        if (File.Exists(path))
        {
            if (hostRepo.TryGetRelativePathToWorkDir(path, out var relPath) == false)
            {
                ConsoleApp.LogError($"'{path}' not inside the workdir");

                throw new OperationCanceledException();
            }

            var fileStatus = hostRepo.GetFileStatus(relPath);
            if (fileStatus != Lg2StatusFlags.LG2_STATUS_CURRENT)
            {
                ConsoleApp.LogError(
                    $"The file either contians outstanding change, or is not managed by git."
                );

                throw new OperationCanceledException();
            }

            var index = hostRepo.GetIndex();
            var entry = index.GetEntry(relPath, 0);

            var oidEntryHex = entry.GetOidPlainRef().GetOidHexDigits();
            ConsoleApp.Log($"Source object id: {oidEntryHex}");

            Lg2Oid targetOid = new();
            tautMapping.GetTautened(entry, ref targetOid);

            ConsoleApp.Log($"Target object id: {targetOid.ToHexDigits()}");

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
            ConsoleApp.LogError($"Not a valid path: '{path}'");
        }
    }

    /// <summary>
    /// Rescan the taut repository and rebuild the taut mapping database.
    /// </summary>
    [Command("--rescan")]
    public void Rescan([FromServices] TautManager tautManager)
    {
        using (var tautRepo = LocateTautRepo())
        {
            // tautManager.Init(tautRepo.GetPath(), null);
        }

        tautManager.RebuildKvStore();
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
            ConsoleApp.LogError($"Failed to locate host repo: not inside a git repository");

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
            ConsoleApp.LogError($"Cannot open taut repo at path '{repoPath}'");

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

internal class ExtraFilter(IServiceProvider serviceProvider, ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    void SetLg2TraceOutput()
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Lg2Trace>();

        Lg2Trace.SetTraceOutput(
            (message) =>
            {
                logger.ZLogTrace($"{message}");
            }
        );
    }

    void ResetLg2TraceOutput()
    {
        Lg2Trace.SetTraceOutput(null);
    }

    public override async Task InvokeAsync(
        ConsoleAppContext context,
        CancellationToken cancellationToken
    )
    {
        ConsoleApp.LogError = msg => Console.Error.WriteLine(msg);

        SetLg2TraceOutput();

        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        finally
        {
            ResetLg2TraceOutput();
        }
    }
}

internal class ProgramSupport
{
    internal static void AddServices(IServiceCollection services)
    {
        services.AddSingleton<GitCli>();
        services.AddSingleton<TautSetup>();
        services.AddSingleton<TautManager>();
        services.AddSingleton<TautMapping>();
        services.AddSingleton<Aes256Cbc1>();
        services.AddSingleton<RecyclableMemoryStreamManager>();
    }
}
