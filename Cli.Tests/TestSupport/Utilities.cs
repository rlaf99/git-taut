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
