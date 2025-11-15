using Lg2.Sharpy;

// Only captures direct output, see https://github.com/xunit/xunit/issues/1730
[assembly: CaptureConsole] 

[assembly: AssemblyFixture(typeof(Cli.Tests.Lg2GlobalFixture))]

namespace Cli.Tests;

public sealed class Lg2GlobalFixture : IDisposable
{
    Lg2Global _lg2 = new();

    public Lg2GlobalFixture()
    {
        _lg2.Init();
    }

    public void Dispose()
    {
        _lg2.Dispose();
    }
}
