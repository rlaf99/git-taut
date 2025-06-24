using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;

[assembly: AssemblyFixture(typeof(Cli.Tests.Lg2GlobalFixture))]
[assembly: AssemblyFixture(typeof(Cli.Tests.LoggerFactoryFixture))]

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

public sealed class LoggerFactoryFixture : IDisposable
{
    ILoggerFactory _loggerFactory;

    public LoggerFactoryFixture()
    {
        _loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddZLoggerInMemory();
        });
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }

    public ILogger<T> CreateLogger<T>() => _loggerFactory.CreateLogger<T>();

    public ILogger GetLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);
}
