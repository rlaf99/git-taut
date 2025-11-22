using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Cli.Tests.TestSupport;

public class CliTestsOptions
{
    public const string SectionName = "CliTests";

    public string TestbedLocation { get; set; } = string.Empty;
}

readonly record struct TempPath(string Value, Action Clear) { }

class Testbed
{
    static string? s_savedPath;

    internal static string GetPath()
    {
        if (s_savedPath is not null)
        {
            return s_savedPath;
        }

        var assemblyName =
            Assembly.GetExecutingAssembly().GetName()?.Name
            ?? throw new InvalidOperationException("Cannot get full name of the assembly.");

        var options = GetOptions();
        var location = options?.TestbedLocation ?? string.Empty;
        if (location == string.Empty)
        {
            location = Path.GetTempPath();
        }

        s_savedPath = Path.Join(location, assemblyName);
        Directory.CreateDirectory(s_savedPath);

        return s_savedPath;
    }

    internal static TempPath CreateDirectory()
    {
        var dirPath = Path.Join(GetPath(), Path.GetRandomFileName() + "_D");
        Directory.CreateDirectory(dirPath);
        return new TempPath() { Value = dirPath, Clear = () => Directory.Delete(dirPath, true) };
    }

    internal static TempPath CreateDirectory(string? suffix = null)
    {
        var dirName = Path.GetRandomFileName();
        if (suffix is not null)
        {
            dirName += suffix;
        }
        var dirPath = Path.Join(GetPath(), dirName);
        Directory.CreateDirectory(dirPath);

        return new TempPath() { Value = dirPath, Clear = () => WipeOutDirectory(dirPath) };
    }

    internal static TempPath CreateFile(string? suffix = null)
    {
        var fileName = Path.GetRandomFileName();
        if (suffix is not null)
        {
            fileName += suffix;
        }
        var filePath = Path.Join(GetPath(), fileName);
        File.Create(filePath);

        return new TempPath() { Value = filePath, Clear = () => File.Delete(filePath) };
    }

    static IConfigurationRoot? s_configRoot;

    static CliTestsOptions? GetOptions()
    {
        s_configRoot ??= new ConfigurationBuilder().AddEnvironmentVariables().Build();

        return s_configRoot.GetSection(CliTestsOptions.SectionName).Get<CliTestsOptions>();
    }

    static void WipeOutDirectory(string dirPath)
    {
        foreach (var file in Directory.GetFiles(dirPath))
        {
            FileInfo info = new(file);
            info.Attributes &= ~FileAttributes.ReadOnly;
            info.Delete();
        }
        foreach (var dir in Directory.GetDirectories(dirPath))
        {
            WipeOutDirectory(dir);
        }
        Directory.Delete(dirPath);
    }
}
