namespace Jellyfin.Plugin.Jellydash.Tests;

/// <summary>
/// Collection-level fixture that cleans up temporary assets created by
/// Jellydash plugin tests once the collection has finished.
///
/// All plugin tests are placed in the same collection so this runs once
/// after the full test suite completes.
/// </summary>
public sealed class TestAssetCleanup : IDisposable
{
    private readonly string _tempRoot;

    public TestAssetCleanup()
    {
        // Tests use this directory for temporary SQLite databases.
        _tempRoot = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; ignore IO errors so test results are
            // not affected by filesystem quirks.
        }
    }
}

/// <summary>
/// Defines a single xUnit collection for all Jellydash plugin tests so
/// that TestAssetCleanup runs once for the whole suite.
/// </summary>
[CollectionDefinition("JellydashPluginTests", DisableParallelization = true)]
public sealed class JellydashPluginTestsCollection : ICollectionFixture<TestAssetCleanup>
{
}
