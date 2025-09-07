using Microsoft.Extensions.Hosting;

namespace Cli.Tests.GitRemoteHelper;

public class RepoOneSetup : IDisposable
{
    public void Dispose()
    {
        // do nothing
    }
}

public sealed class CloneTests : IClassFixture<RepoOneSetup>, IDisposable
{
    HostBuilderFixture _hostBuilder;

    IHost _host;

    public CloneTests(HostBuilderFixture hostBuilder)
    {
        _hostBuilder = hostBuilder;
    }

    [Fact]
    public void CloneAsTaut()
    {
        // Given

        // When

        // Then
    }

    public void Dispose()
    {
        // do nothing
    }
}
