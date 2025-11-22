namespace Cli.Tests.TestSupport;

public class SetCurrentDirectoryFixture : IDisposable
{
    public void Dispose() { }
}

[CollectionDefinition("SetCurrentDirectory")]
public class SetCurrentDirectoryCollection : ICollectionFixture<SetCurrentDirectoryFixture> { }
