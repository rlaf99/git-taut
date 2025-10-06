using Lg2.Sharpy;
using Microsoft.Extensions.Hosting;
using ProgramExtras;

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

HostApplicationBuilderSettings hostBuilderSettings = new();
var hostBuilder = Host.CreateEmptyApplicationBuilder(hostBuilderSettings);

hostBuilder.AddGitTautConfiguration();
hostBuilder.AddGitTautServices();
hostBuilder.AddGitTautCommandActions();
hostBuilder.AddGitTautLogging();

var host = hostBuilder.Build();

ProgramCommandLine progCommands = new(host);
return progCommands.Parse(args);

#if false
ConsoleApp.Version = "alpha-0.0.1";

ConsoleApp.Timeout = TimeSpan.FromMicroseconds(800);
#endif
