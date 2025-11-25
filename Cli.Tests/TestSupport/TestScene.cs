namespace Cli.Tests.TestSupport;

class TestScene : IDisposable
{
    const string NameSuffix = ".S";

    readonly TempPath _path;

    public string Location => _path.Value;

    public TestScene()
    {
        _path = Testbed.CreateDirectory(NameSuffix);
    }

    public bool ShouldPreserve { get; private set; }

    public void PreserveContentWhenFailed(ITestOutputHelper? output = null)
    {
        var state = TestContext.Current.TestState;
        if (state?.Result == TestResult.Failed)
        {
            ShouldPreserve = true;

            if (output is not null)
            {
                var caseName = TestContext.Current.TestCase?.TestCaseDisplayName;
                output.WriteLine($"Preserve '{Location}' for failed case {caseName}.");
            }
        }
    }

    public void PreserveContentWhenAsked(ITestOutputHelper? output = null)
    {
        ShouldPreserve = true;

        if (output is not null)
        {
            var caseName = TestContext.Current.TestCase?.TestCaseDisplayName;
            output.WriteLine($"Preserve'{Location}' for case {caseName} as asked.");
        }
    }

    bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (ShouldPreserve == false)
        {
            var workingDir = Directory.GetCurrentDirectory();
            var cleanupDir = Path.GetFullPath(_path.Value);

            if (workingDir.StartsWith(cleanupDir))
            {
                Directory.SetCurrentDirectory(Testbed.GetPath());
            }

            _path.Clear();
        }
    }

    ~TestScene() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
