using System.CommandLine;
using Git.Taut;
using Lg2.Sharpy;

using var lg2Global = new Lg2Global();

try
{
    lg2Global.Init();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}

using var host = GitTautHostBuilder.BuildHost();

ProgramCommandLine progCli = new(host);

ParserConfiguration parserConfiguration = new() { ResponseFileTokenReplacer = null };
var parseResult = progCli.ParseForGitTaut(args, parserConfiguration);

InvocationConfiguration invocationConfiguration = new()
{
    ProcessTerminationTimeout = TimeSpan.FromMilliseconds(800),
};

return await parseResult.InvokeAsync(invocationConfiguration);
