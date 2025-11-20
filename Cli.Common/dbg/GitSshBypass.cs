using System.CommandLine;

namespace Git.Taut;

class GitSshBypass
{
    Option<string> _optionOption = new("--option", "-o"); // { Arity = ArgumentArity.ZeroOrMore };
    Argument<string> _addressArgument = new("address");
    Argument<string> _executeArgument = new("execute");

    const string _localhost = "localhost";

    internal void HandleParseResult(ParseResult parseResult)
    {
        var option = parseResult.GetValue(_optionOption);
        if (option is not null)
        {
            Console.WriteLine($"{option}");
        }

        var address = parseResult.GetValue(_addressArgument);
        if (address != _localhost)
        {
            throw new InvalidOperationException($"Only {_localhost} is supported");
        }

        var execute = parseResult.GetValue(_executeArgument);
        Console.WriteLine($"{execute}");
    }

    internal Command BuildCommand()
    {
        Command command = new("dbg-ssh-bypass", "Serve a repository through mocked ssh")
        {
            _optionOption,
            _addressArgument,
            _executeArgument,
        };

        command.SetAction(HandleParseResult);

        return command;
    }
}
