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
        var tautRepo = LocateTautRepo();

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
    /// Regain a tautened file, output regained content to stdout.
    /// </summary>
    /// <param name="file">The tautened file to regain.</param>
    [Command("--regain")]
    public void Regain([FromServices] Aes256Cbc1 cipher, [Argument] string file)
    {
        cipher.Init();

        try
        {
            using var fileStream = File.OpenRead(file);
            using var outputStream = Console.OpenStandardOutput();
            cipher.Decrypt(outputStream, fileStream, false);
        }
        catch (Exception ex)
        {
            ConsoleApp.LogError($"Failed to regain '{file}': {ex.Message}");

            throw new OperationCanceledException();
        }
    }

    Lg2Repository LocateTautRepo()
    {
        var currentDir = Directory.GetCurrentDirectory();
        if (Lg2Repository.TryDiscover(currentDir, out var hostRepo) == false)
        {
            ConsoleApp.LogError($"Not inside a git repository");

            throw new OperationCanceledException();
        }

        var tautRepoFullPath = Path.Join(hostRepo.GetPath(), GitRepoLayout.TautRepoDir);
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
