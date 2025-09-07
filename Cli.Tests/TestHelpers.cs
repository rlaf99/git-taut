using System.Reflection;
using Microsoft.Extensions.Configuration;
using Xunit.v3;

namespace Cli.Tests;

public class CliTestsOptions
{
    public const string SectionName = "CliTests";

    public string TestbedLocation { get; set; } = string.Empty;
}

internal readonly record struct TempPath(string Value, Action Clear) : IDisposable
{
    public readonly void Dispose()
    {
        Clear();
    }
}

internal class Testbed
{
    const string TestbedDir = "testbed";

    static string? s_savedPath;

    internal static string GetPath()
    {
        if (s_savedPath is null)
        {
            var assemblyName =
                Assembly.GetExecutingAssembly().GetName()?.Name
                ?? throw new InvalidOperationException("Cannot get full name of the assembly.");

            var options = GetOptions();
            var location = options?.TestbedLocation ?? string.Empty;
            if (location == string.Empty)
            {
                location = Path.GetTempPath();
            }

            s_savedPath = Path.Join(location, assemblyName, TestbedDir);
            Directory.CreateDirectory(s_savedPath);
        }

        return s_savedPath;
    }

    // internal static TempPath CreateGitRepo()
    // {
    //     var repoPath = Path.Join(GetPath(), Path.GetRandomFileName() + "_G");
    //     Repository.Init(repoPath);
    //     return new TempPath() { Value = repoPath, Clear = () => Directory.Delete(repoPath, true) };
    // }

    internal static TempPath CreateDirectory()
    {
        var dirPath = Path.Join(GetPath(), Path.GetRandomFileName() + "_D");
        Directory.CreateDirectory(dirPath);
        return new TempPath() { Value = dirPath, Clear = () => Directory.Delete(dirPath, true) };
    }

    internal static TempPath CreateFile()
    {
        var filePath = Path.Join(GetPath(), Path.GetRandomFileName() + "_F");
        File.Create(filePath);
        return new TempPath() { Value = filePath, Clear = () => File.Delete(filePath) };
    }

    static IConfigurationRoot? s_configRoot;

    static CliTestsOptions? GetOptions()
    {
        if (s_configRoot is null)
        {
            s_configRoot = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        }

        return s_configRoot.GetSection(CliTestsOptions.SectionName).Get<CliTestsOptions>();
    }
}

partial class GitRepoFixture<TDerived>
{
    readonly TempPath _repoPath;

    public string RepoPath => _repoPath.Value;

    public bool ShouldClear { get; set; } = true;

    public const string DefaultPasskey = "123";

    public GitRepoFixture()
    {
        // _repoPath = Testbed.CreateGitRepo();
    }

    // internal DataStore CreateTempDataStore()
    // {
    //     var baseDir = Path.GetRandomFileName();
    //     var basePath = Path.Join(RepoPath, baseDir);
    //     Directory.CreateDirectory(basePath);

    //     var config = new DataStore.ExtraConfig() { Passkey = DefaultPasskey };
    //     var dataStore = DataStore.Init(basePath, config);

    //     return dataStore;
    // }

    public void PreserveSceneWhenFailed(ITestOutputHelper output)
    {
        var state = TestContext.Current.TestState;
        if (state?.Result == TestResult.Failed)
        {
            ShouldClear = false;

            var caseName = TestContext.Current.TestCase?.TestClassSimpleName;

            output.WriteLine($"Not cleanup '{RepoPath}' due to the case {caseName} has failed.");
        }
    }

    public void ClearDataStoreBaseIfSucceeded(ITestOutputHelper output, string basePath)
    {
        var state = TestContext.Current.TestState;
        if (state?.Result == TestResult.Failed)
        {
            ShouldClear = false;

            var caseName = TestContext.Current.TestCase?.TestClassSimpleName;
            output.WriteLine(
                $"Not cleanup DataStore '{basePath}' due to the case {caseName} has failed."
            );
        }
        else
        {
            Directory.Delete(basePath, true);
        }
    }
}

partial class GitRepoFixture<TDerived> : IDisposable
{
    protected bool Disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        if (disposing)
        {
            if (ShouldClear)
            {
                _repoPath.Clear();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
