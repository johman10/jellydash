using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash;
using Jellyfin.Plugin.Jellydash.Configuration;
using Jellyfin.Plugin.Jellydash.ScheduledTasks;
using Jellyfin.Plugin.Jellydash.Services;
using Xunit;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public sealed class JellydashHistoryCleanupTaskTests : IDisposable
{
    private readonly string _originalDbPath;
    private readonly Plugin? _originalPluginInstance;
    private readonly string _dbPathForTest;

    public JellydashHistoryCleanupTaskTests()
    {
        // Preserve any existing plugin instance so we can restore it.
        _originalPluginInstance = Plugin.Instance;

        // Ensure the history repository uses the same temp db path as other tests.
        _originalDbPath = HistoryRepository.DatabasePathOverride ?? string.Empty;
        if (string.IsNullOrEmpty(HistoryRepository.DatabasePathOverride))
        {
            var tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "JellydashPluginTests");
            System.IO.Directory.CreateDirectory(tempRoot);
            _dbPathForTest = System.IO.Path.Combine(tempRoot, "history_cleanup.db");
            HistoryRepository.DatabasePathOverride = _dbPathForTest;
        }
        else
        {
            _dbPathForTest = HistoryRepository.DatabasePathOverride;
        }
    }

    public void Dispose()
    {
        // Restore plugin instance and db override after each test.
        typeof(Plugin)
            .GetProperty("Instance")!
            .SetValue(null, _originalPluginInstance);

        HistoryRepository.DatabasePathOverride = _originalDbPath;

    }

    [Fact]
    public async Task ExecuteAsync_NoConfiguration_DoesNothing()
    {
        // Arrange: no Plugin.Instance set, so Configuration is null.
        Plugin? pluginBefore = Plugin.Instance;
        typeof(Plugin).GetProperty("Instance")!.SetValue(null, null);

        var repo = new HistoryRepository();
        var task = new JellydashHistoryCleanupTask(repo);

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
        var repo = new HistoryRepository();
        await SeedSingleEntryAsync(repo);

        var fakePlugin = new FakePlugin(new PluginConfiguration
        {
            EnableRetention = false,
            HistoryRetentionDays = 30
        });
        typeof(Plugin).GetProperty("Instance")!.SetValue(null, fakePlugin);

        var task = new JellydashHistoryCleanupTask(repo);
        var progress = new TestProgress();

        var beforeEntries = await repo.GetRecentAsync(DateTime.MinValue, CancellationToken.None);

        // Act
        await task.ExecuteAsync(progress, CancellationToken.None);

        // Assert: cleanup did not delete any entries when retention is disabled.
        var entries = await repo.GetRecentAsync(DateTime.MinValue, CancellationToken.None);
        Assert.Equal(beforeEntries.Count, entries.Count);
        Assert.Equal(1.0, progress.Value);
    }

    [Fact]
    public async Task ExecuteAsync_RetentionEnabled_DeletesOldEntries()
    {
        // Arrange
        var repo = new HistoryRepository();

        // One very old entry and one recent entry.
        await SeedEntryAsync(repo, DateTime.UtcNow.AddDays(-60));
        await SeedEntryAsync(repo, DateTime.UtcNow.AddDays(-1));

        var fakePlugin = new FakePlugin(new PluginConfiguration
        {
            EnableRetention = true,
            HistoryRetentionDays = 30
        });
        typeof(Plugin).GetProperty("Instance")!.SetValue(null, fakePlugin);

        var task = new JellydashHistoryCleanupTask(repo);
        var progress = new TestProgress();

        // Act
        await task.ExecuteAsync(progress, CancellationToken.None);

        // Assert: no entries older than the retention window remain.
        var entries = await repo.GetRecentAsync(DateTime.MinValue, CancellationToken.None);
        var cutoff = DateTime.UtcNow - TimeSpan.FromDays(30);
        Assert.All(entries, e => Assert.True(e.EndUtc >= cutoff));
        Assert.Equal(1.0, progress.Value);
    }

    private static async Task SeedSingleEntryAsync(HistoryRepository repo)
    {
        await SeedEntryAsync(repo, DateTime.UtcNow.AddDays(-1));
    }

    private static async Task SeedEntryAsync(HistoryRepository repo, DateTime endUtc)
    {
        var startUtc = endUtc.AddMinutes(-10);
        var entry = new Jellyfin.Plugin.Jellydash.Models.HistoryEntry
        {
            UserId = Guid.NewGuid(),
            UserName = "User",
            ItemId = Guid.NewGuid(),
            MediaType = "Movie",
            ItemName = "Item",
            StartUtc = startUtc,
            EndUtc = endUtc,
            StartPercentage = 0,
            EndPercentage = 100
        };

        await repo.AppendAsync(entry, CancellationToken.None);
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

        public object DeserializeFromStream(Type type, System.IO.Stream stream) => Activator.CreateInstance(type)!;

        public void SerializeToFile(object obj, string file)
        {
            // No-op for tests.
        }

        public void SerializeToStream(object obj, System.IO.Stream stream)
        {
            // No-op for tests.
        }
    }
}
