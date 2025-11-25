using Cli.Tests.TestSupport;
using Git.Taut;
using Lg2.Sharpy;
using static Cli.Tests.TestSupport.TestScenePlanConstants;

namespace Cli.Tests.RemoteHelper;

public sealed class UpdateTests(ITestOutputHelper testOutput) : IDisposable
{
    TestScenePlan _plan = new(testOutput);

    public void Dispose()
    {
        _plan.PreserveContentWhenFailed(testOutput);
        _plan.Dispose();
    }

    [Fact]
    public void Test1()
    {
        _plan.SetupRepo0();
        _plan.SetupRepo1();
    }
}
