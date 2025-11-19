using Git.Taut;
using Microsoft.Extensions.Hosting;

namespace Git.Remote.Taut;

sealed class GitRemoteTautHostBuilder
{
    public static IHost BuildHost()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddForGitRemoteTaut();

        return builder.Build();
    }
}
