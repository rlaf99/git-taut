using System.CommandLine;

namespace Cli.Tests.TestSupport;

static class ITestOutputHelperExtensions
{
    internal static void DumpError(
        this ITestOutputHelper testOutput,
        InvocationConfiguration invocationConfiguration
    )
    {
        var erorrText = invocationConfiguration.Error.ToString();

        if (string.IsNullOrEmpty(erorrText))
        {
            testOutput.WriteLine($"No error output from {nameof(InvocationConfiguration)}");
        }
        else
        {
            testOutput.WriteLine($"Error output from {nameof(InvocationConfiguration)}:");
            testOutput.WriteLine(erorrText);
        }
    }
}

ref struct PushDirectory : IDisposable
{
    string? _pushed;

    internal PushDirectory(string? targetDirectory = null)
    {
        _pushed = Directory.GetCurrentDirectory();

        if (targetDirectory is not null)
        {
            Directory.SetCurrentDirectory(targetDirectory);
        }
    }

    public void Dispose()
    {
        var pushed = Interlocked.Exchange(ref _pushed, null);
        if (pushed is null)
        {
            return;
        }

        Directory.SetCurrentDirectory(pushed);
    }
}
