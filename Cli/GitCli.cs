using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class GitCliException : Exception
{
    internal GitCliException(string message)
        : base(message) { }
}

class GitCli(ILogger<GitCli> logger)
{
    string _envAlternateObjDirs = string.Empty;
    List<string> _alternateObjDirs = [];
    internal IReadOnlyList<string> AlternateObjectDirectories
    {
        get { return _alternateObjDirs; }
        set
        {
            _alternateObjDirs = [.. value];
            _envAlternateObjDirs = string.Join(Path.PathSeparator, value);
        }
    }

    void SetEnvironmentAlternativeObjectDirectories(ProcessStartInfo startInfo)
    {
        if (string.IsNullOrEmpty(_envAlternateObjDirs))
        {
            startInfo.Environment.Remove(KnownEnvironVars.GitAlternateObjectDirectories);
        }
        else
        {
            startInfo.Environment[KnownEnvironVars.GitAlternateObjectDirectories] =
                _envAlternateObjDirs;

            logger.ZLogTrace(
                $"Set environment '{KnownEnvironVars.GitAlternateObjectDirectories}' to '{_envAlternateObjDirs}'"
            );
        }
    }

    void EnsureExitCode(int exitCode)
    {
        if (exitCode != 0)
        {
            throw new GitCliException($"Process exited with non-zero code {exitCode}");
        }
    }

    internal void Run(params string[] args)
    {
        var startInfo = new ProcessStartInfo("git") { Arguments = string.Join(" ", args) };

        logger.ZLogTrace($"Running git with {args.Length} arguments '{startInfo.Arguments}'");

        using var process = new Process() { StartInfo = startInfo };

        process.Start();
        process.WaitForExit();

        EnsureExitCode(process.ExitCode);
    }

    internal void Execute(List<string> inputLines, params string[] args)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        SetEnvironmentAlternativeObjectDirectories(startInfo);

        logger.ZLogTrace($"Running git with {args.Length} arguments '{startInfo.Arguments}'");

        using var process = new Process() { StartInfo = startInfo };

        static void ErrorDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
                Console.Error.WriteLine(args.Data);
        }

        // process.OutputDataReceived += OutputDataReceiver; // ignore the output
        process.ErrorDataReceived += ErrorDataReceiver;

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        foreach (var line in inputLines)
        {
            process.StandardInput.WriteLine(line);
        }

        process.StandardInput.Close();

        process.WaitForExit();

        EnsureExitCode(process.ExitCode);
    }

    internal void Execute(params string[] args)
    {
        Execute([], args);
    }

    internal void Execute(
        Action<StreamWriter>? inputProvider,
        Action<string>? dataReceiver,
        Action<string>? errorDataReceiver,
        params string[] args
    )
    {
        var startInfo = new ProcessStartInfo("git")
        {
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        SetEnvironmentAlternativeObjectDirectories(startInfo);

        logger.ZLogTrace($"Executing git with {args.Length} arguments '{startInfo.Arguments}'");

        List<string> result = [];

        using var process = new Process() { StartInfo = startInfo };

        void OutputDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null && dataReceiver is not null)
            {
                dataReceiver(args.Data);
            }
        }

        void ErrorDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null && errorDataReceiver is not null)
            {
                errorDataReceiver(args.Data);
            }
        }

        process.OutputDataReceived += OutputDataReceiver;
        process.ErrorDataReceived += ErrorDataReceiver;

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (inputProvider is not null)
        {
            inputProvider(process.StandardInput);
        }

        process.StandardInput.Close();

        process.WaitForExit();

        EnsureExitCode(process.ExitCode);
    }

    internal List<string> ExecuteForOutput2(List<string> inputLines, params string[] args)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        SetEnvironmentAlternativeObjectDirectories(startInfo);

        logger.ZLogTrace($"Running git with {args.Length} arguments '{startInfo.Arguments}'");

        List<string> result = [];

        using var process = new Process() { StartInfo = startInfo };

        void OutputDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
            {
                result.Add(args.Data);
            }
        }

        static void ErrorDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
            {
                Console.Error.WriteLine(args.Data);
            }
        }

        process.OutputDataReceived += OutputDataReceiver;
        process.ErrorDataReceived += ErrorDataReceiver;

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        foreach (var line in inputLines)
        {
            process.StandardInput.WriteLine(line);
        }
        process.StandardInput.Close();

        process.WaitForExit();

        EnsureExitCode(process.ExitCode);

        return result;
    }

    internal List<string> ExecuteForOutput(List<string> inputLines, params string[] args)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        SetEnvironmentAlternativeObjectDirectories(startInfo);

        logger.ZLogTrace($"Running git with {args.Length} arguments '{startInfo.Arguments}'");

        List<string> result = [];

        using var process = new Process() { StartInfo = startInfo };

        void OutputDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
            {
                result.Add(args.Data);
            }
        }

        static void ErrorDataReceiver(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is not null)
            {
                Console.Error.WriteLine(args.Data);
            }
        }

        process.OutputDataReceived += OutputDataReceiver;
        process.ErrorDataReceived += ErrorDataReceiver;

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        foreach (var line in inputLines)
        {
            process.StandardInput.WriteLine(line);
        }
        process.StandardInput.Close();

        process.WaitForExit();

        EnsureExitCode(process.ExitCode);

        return result;
    }

    internal List<string> ExecuteForOutput(params string[] args)
    {
        return ExecuteForOutput([], args);
    }
}
