using System.CommandLine;
using Lg2.Sharpy;
using Microsoft.Extensions.Hosting;
using ProgramHelpers;

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
var parseResult = progCli.Parse(args, parserConfiguration);

InvocationConfiguration invocationConfiguration = new()
{
    ProcessTerminationTimeout = TimeSpan.FromMilliseconds(800),
};

return await parseResult.InvokeAsync(invocationConfiguration);
