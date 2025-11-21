using System.CommandLine;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Git.Taut;

class GitSshBypassCommandBuilder(ILoggerFactory loggerFactory, GitCli gitCli) : CommandActionsBase
{
    internal static Option<string> _optionOption = new("--option", "-o"); // { Arity = ArgumentArity.ZeroOrMore };
    internal static Argument<string> _addressArgument = new("address");
    internal static Argument<string> _executeArgument = new("execute");

    internal Command BuildCommand()
    {
        Command command = new("dbg-ssh-bypass", "Serve a repository through mocked ssh")
        {
            _optionOption,
            _addressArgument,
            _executeArgument,
        };

        command.SetAction(
            (parseResult) =>
            {
                GitSshBypass sshBypass = new(loggerFactory.CreateLogger<GitSshBypass>(), gitCli);

                if (sshBypass is null)
                {
                    throw new InvalidOperationException(
                        $"Failed to get service '{nameof(GitSshBypass)}'"
                    );
                }
                sshBypass.HandleParseResult(parseResult);
            }
        );

        return command;
    }
}

class GitSshBypass(ILogger<GitSshBypass> logger, GitCli gitCli)
{
    const string _localhost = "localhost";
    const string _optSendEnv = "SendEnv=";
    const string _gitUploadPack = "git-upload-pack";
    const string _gitReceivePack = "git-receive-pack";

    HashSet<string> _envs = [];

    void HandleOption(string option)
    {
        if (option.StartsWith(_optSendEnv))
        {
            var val = option[_optSendEnv.Length..];

            _envs.Add(val);
        }
        else
        {
            throw new InvalidOperationException($"Unknown option '{option}'");
        }
    }

    string Unquote(string input)
    {
        StringBuilder builder = new();

        bool singleQuoted = false;
        bool nextEscaped = false;

        foreach (var ch in input)
        {
            if (singleQuoted)
            {
                if (ch == '\'')
                {
                    singleQuoted = false;
                }
                else
                {
                    builder.Append(ch);
                }

                continue;
            }

            if (nextEscaped)
            {
                nextEscaped = false;

                builder.Append(ch);

                continue;
            }

            if (ch == '\'')
            {
                singleQuoted = true;

                continue;
            }

            if (ch == '\\')
            {
                nextEscaped = true;

                continue;
            }

            builder.Append(ch);
        }

        var result = builder.ToString();

        return result;
    }

    void HandleExecute(string execute)
    {
        if (execute.StartsWith(_gitUploadPack))
        {
            var quotedArg = execute[_gitUploadPack.Length..].TrimStart();
            var arg = Unquote(quotedArg);

            logger.ZLogDebug($"Execute {_gitUploadPack} with {arg}");

            if (arg.Length >= 3 && Path.IsPathFullyQualified(arg[1..]))
            {
                arg = arg[1..];
            }

            gitCli.Run("upload-pack", arg);
        }
        else if (execute.StartsWith(_gitReceivePack))
        {
            var quotedArg = execute[_gitReceivePack.Length..].TrimStart();
            var arg = Unquote(quotedArg);

            logger.ZLogDebug($"Execute {_gitReceivePack} with {arg}");

            if (arg.Length >= 3 && Path.IsPathFullyQualified(arg[1..]))
            {
                arg = arg[1..];
            }

            gitCli.Run("receive-pack", arg);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported '{execute}'");
        }
    }

    internal void HandleParseResult(ParseResult parseResult)
    {
        var option = parseResult.GetValue(GitSshBypassCommandBuilder._optionOption);
        if (option is not null)
        {
            HandleOption(option);
        }

        var address = parseResult.GetRequiredValue(GitSshBypassCommandBuilder._addressArgument);
        if (address != _localhost)
        {
            throw new InvalidOperationException(
                $"Invalid address '{address}', only {_localhost} is supported"
            );
        }

        var execute = parseResult.GetRequiredValue(GitSshBypassCommandBuilder._executeArgument);
        HandleExecute(execute);
    }
}
