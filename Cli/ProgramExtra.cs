using ConsoleAppFramework;
using Git.Taut;
using Lg2.Sharpy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

internal class ExtraCommands
{
    /// <summary>Print internal information.</summary>
    [Command("--internal-info")]
    public void InternalInfo()
    {
        var lg2Version = Lg2Global.Version;
        ConsoleApp.Log($"LibGit2 Version: {lg2Version}");
    }

    /// <summary>
    ///  Invoke git on the taut repo.
    /// </summary>
    [Command("--repo")]
    public void Repo([FromServices] GitCli gitCli, [Argument] params string[] args)
    {
        using var tautRepo = LocateTautRepo();

        string[] gitArgs = ["--git-dir", tautRepo.GetPath(), .. args];
        try
        {
            gitCli.Run(gitArgs);
        }
        catch (Exception)
        {
            ConsoleApp.LogError($"Failed to run `{string.Join(" ", gitArgs)}`");
        }
    }

    /// <summary>
    /// Regain a tautened file and dump its content to stdout.
    /// </summary>
    /// <param name="file">The tautened file to regain.</param>
    [Command("--regain")]
    public void Regain([FromServices] Aes256Cbc1 cipher, [Argument] string file)
    {
        cipher.Init();

        try
        {
            using var fileStream = File.OpenRead(file);
            var decryptor = cipher.CreateDecryptor(fileStream, false);

            using var outputStream = Console.OpenStandardOutput();
            decryptor.WriteToEnd(outputStream);
        }
        catch (Exception ex)
        {
            ConsoleApp.LogError($"Failed to regain '{file}': {ex.Message}");

#if DEBUG
            if (ex.StackTrace is not null)
            {
                ConsoleApp.LogError($"{ex.StackTrace}");
            }
#endif
            throw new OperationCanceledException();
        }
    }

    /// <summary>
    /// Rescan the taut repo and rebuild mapping.
    /// </summary>
    [Command("--rescan")]
    public void Rescan([FromServices] TautManager tautManager)
    {
        using (var tautRepo = LocateTautRepo())
        {
            tautManager.Open(tautRepo.GetPath());
        }

        tautManager.RebuildKvStore();
    }

    Lg2Repository LocateTautRepo()
    {
        var currentDir = Directory.GetCurrentDirectory();
        if (Lg2Repository.TryDiscover(currentDir, out var hostRepo) == false)
        {
            ConsoleApp.LogError($"Not inside a git repository");

            throw new OperationCanceledException();
        }

        var tautRepoFullPath = Path.Join(hostRepo.GetPath(), GitRepoHelper.TautRepoDir);
        var tautRepoRelPath = Path.GetRelativePath(currentDir, tautRepoFullPath);

        Lg2Repository? tautRepo = null;
        try
        {
            tautRepo = Lg2Repository.New(tautRepoRelPath);
        }
        catch (Exception)
        {
            ConsoleApp.LogError($"Cannot find taut repo at path '{tautRepoRelPath}'");

            throw new OperationCanceledException();
        }
        return tautRepo!;
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
