using Jellyfin.Plugin.Jellydash.Configuration;
using Jellyfin.Plugin.Jellydash.ScheduledTasks;
using Jellyfin.Plugin.Jellydash.Services;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public sealed class JellydashCleanupTaskTests : IDisposable
{
    private readonly Plugin? _originalPluginInstance;
    private readonly DatabaseHelper DatabaseHelper;

    public JellydashCleanupTaskTests()
    {
        // Preserve any existing plugin instance so we can restore it.
        _originalPluginInstance = Plugin.Instance;

        // Ensure the activity repository uses the same temp db path as other tests.
        var tempRoot = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
        Directory.CreateDirectory(tempRoot);
        DatabaseHelper = new DatabaseHelper(Path.Combine(tempRoot, "activity_cleanup.db"));
        DatabaseHelper.Initialize();
    }

    public void Dispose()
    {
        // Restore plugin instance and db override after each test.
        typeof(Plugin)
            .GetProperty("Instance")!
            .SetValue(null, _originalPluginInstance);
    }

    [Fact]
    public async Task ExecuteAsync_NoConfiguration_DoesNothing()
    {
        // Arrange: no Plugin.Instance set, so Configuration is null.
        Plugin? pluginBefore = Plugin.Instance;
        typeof(Plugin).GetProperty("Instance")!.SetValue(null, null);

        var repo = new PlaybackEntryRepository(DatabaseHelper);
        var task = new JellydashCleanupTask(repo);

        var progress = new TestProgress();

        // Act
        await task.ExecuteAsync(progress, CancellationToken.None);

        // Assert
        Assert.Equal(1.0, progress.Value);

        // Restore any previous plugin instance explicitly for safety.
        typeof(Plugin).GetProperty("Instance")!.SetValue(null, pluginBefore);
    }

    [Fact]
    public async Task ExecuteAsync_RetentionDisabled_DoesNotDelete()
    {
        // Arrange
        var repo = new PlaybackEntryRepository(DatabaseHelper);
        var playbackId = await SeedSingleEntryAsync(repo);

        var fakePlugin = new FakePlugin(new PluginConfiguration
        {
            EnableRetention = false,
            RetentionDays = 30
        });
        typeof(Plugin).GetProperty("Instance")!.SetValue(null, fakePlugin);

        var task = new JellydashCleanupTask(repo);
        var progress = new TestProgress();

        var beforeEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(beforeEntry);

        // Act
        await task.ExecuteAsync(progress, CancellationToken.None);
        Assert.Equal(1.0, progress.Value);

        // Assert: cleanup did not delete any entries when retention is disabled.
        var afterEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(afterEntry);
    }

    [Fact]
    public async Task ExecuteAsync_RetentionEnabled_DeletesOldEntries()
    {
        // Arrange
        var repo = new PlaybackEntryRepository(DatabaseHelper);

        // One very old entry and one recent entry.
        var olderPlaybackId = await SeedEntryAsync(repo, DateTime.UtcNow.AddDays(-60));
        var newerPlaybackId = await SeedEntryAsync(repo, DateTime.UtcNow.AddDays(-1));

        var fakePlugin = new FakePlugin(new PluginConfiguration
        {
            EnableRetention = true,
            RetentionDays = 30
        });
        typeof(Plugin).GetProperty("Instance")!.SetValue(null, fakePlugin);

        var task = new JellydashCleanupTask(repo);
        var progress = new TestProgress();

        // Act
        await task.ExecuteAsync(progress, CancellationToken.None);
        Assert.Equal(1.0, progress.Value);

        var newerEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(newerPlaybackId, CancellationToken.None);
        Assert.NotNull(newerEntry);

        // Assert: no entries older than the retention window remain.
        var olderEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(olderPlaybackId, CancellationToken.None);
        Assert.Null(olderEntry);
    }

    private static async Task<Guid> SeedSingleEntryAsync(PlaybackEntryRepository repo)
    {
        return await SeedEntryAsync(repo, DateTime.UtcNow.AddDays(-1));
    }

    private static async Task<Guid> SeedEntryAsync(PlaybackEntryRepository repo, DateTime endUtc)
    {
        var startUtc = endUtc.AddMinutes(-10);
        var entry = new Models.PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            ContentKind = Models.ContentKind.Movie,
            Title = "Item",
            UserId = Guid.NewGuid(),
            UserName = "User",
            ClientName = "TestClient",
            DeviceName = "TestDevice",
            StartUtc = startUtc,
            EndUtc = endUtc,
        };

        await repo.AppendAsync(entry, CancellationToken.None);
        return entry.PlaybackId;
    }

    private sealed class TestProgress : IProgress<double>
    {
        public double Value { get; private set; }

        public void Report(double value)
        {
            Value = value;
        }
    }

    private sealed class FakePlugin : Plugin
    {
        public FakePlugin(PluginConfiguration configuration)
            : base(new FakeApplicationPaths(), new FakeXmlSerializer())
        {
            Configuration = configuration;
        }
    }

    private sealed class FakeApplicationPaths : MediaBrowser.Common.Configuration.IApplicationPaths
    {
        public string ProgramDataPath => System.IO.Path.GetTempPath();

        public string ProgramSystemPath => System.IO.Path.GetTempPath();

        public string WebPath => System.IO.Path.GetTempPath();

        public string DataPath => ProgramDataPath;

        public string ImageCachePath => ProgramDataPath;

        public string PluginsPath => ProgramDataPath;

        public string PluginConfigurationsPath => ProgramDataPath;

        public string LogDirectoryPath => ProgramDataPath;

        public string ConfigurationDirectoryPath => ProgramDataPath;

        public string SystemConfigurationFilePath => System.IO.Path.Combine(ProgramDataPath, "system.xml");

        public string CachePath => ProgramDataPath;

        public string TempDirectory => ProgramDataPath;

        public string VirtualDataPath => ProgramDataPath;

        public string TrickplayPath => ProgramDataPath;

        public string BackupPath => ProgramDataPath;

        public void MakeSanityCheckOrThrow()
        {
            // No-op in tests.
        }

        public void CreateAndCheckMarker(string path, string markerName, bool recursive)
        {
            // No-op in tests.
        }
    }

    private sealed class FakeXmlSerializer : MediaBrowser.Model.Serialization.IXmlSerializer
    {
        public object DeserializeFromBytes(Type type, byte[] buffer) => Activator.CreateInstance(type)!;

        public object DeserializeFromFile(Type type, string file) => Activator.CreateInstance(type)!;

        public object DeserializeFromStream(Type type, Stream stream) => Activator.CreateInstance(type)!;

        public void SerializeToFile(object obj, string file)
        {
            // No-op for tests.
        }

        public void SerializeToStream(object obj, Stream stream)
        {
            // No-op for tests.
        }
    }
}
