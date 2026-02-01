using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellydash.Events;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public sealed class PlaybackTrackerTests
{
    private static readonly DatabaseHelper DatabaseHelper;

    static PlaybackTrackerTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
        Directory.CreateDirectory(tempDir);
        DatabaseHelper = new DatabaseHelper(tempDir);
        DatabaseHelper.Initialize();
    }

    private static async Task<PlaybackEntryRepository> CreateRepositoryAsync()
    {
        var repo = new PlaybackEntryRepository(DatabaseHelper);
        await repo.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddYears(1000), CancellationToken.None);
        return repo;
    }

    private static PlaybackTracker CreateTracker(int? minResumePct = 20, int? maxResumePct = 90)
    {
        var logger = Mock.Of<ILogger<PlaybackTracker>>();
        var tempPath = Path.Combine(Path.GetTempPath(), "PlaybackTrackerTests");
        var imageCaptureService = new ImageCaptureService(
            Mock.Of<IImageProcessor>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<ILogger<ImageCaptureService>>(),
            tempPath);

        var mockConfig = new Mock<IServerConfigurationManager>();
        var configuration = new MediaBrowser.Model.Configuration.ServerConfiguration();
        configuration.MinResumePct = minResumePct ?? 20;
        configuration.MaxResumePct = maxResumePct ?? 90;
        mockConfig.Setup(c => c.Configuration).Returns(configuration);

        return new PlaybackTracker(logger, DatabaseHelper, imageCaptureService, mockConfig.Object);
    }

    [Fact]
    public async Task OnEvent_StartAndStop_WritesActivity()
    {
        var repo = await CreateRepositoryAsync();
        var logger = CreateTracker();

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 10_000_000L, 0L);
        await logger.OnEvent(startArgs);

        var startEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(PlaybackEntry.GeneratePlaybackId(startArgs.Session.Id, startArgs.Session.PlaylistItemId, startArgs.MediaInfo.Id), CancellationToken.None);
        Assert.NotNull(startEntry);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 10_000_000L, 5_000_000L, startArgs.Session.Id, startArgs.Session.PlaylistItemId);
        await logger.OnEvent(stopArgs);

        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id), CancellationToken.None);
        Assert.NotNull(entry);

        Assert.Equal(userId, entry.UserId);
        Assert.Equal(itemId, entry.ItemId);
        Assert.True(entry.IsCompleted);
    }

    [Fact]
    public async Task OnEvent_StopWithoutStart_StillWritesEntry()
    {
        var repo = await CreateRepositoryAsync();
        var logger = CreateTracker();

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 10_000_000L, 2_500_000L, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        await logger.OnEvent(stopArgs);

        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id), CancellationToken.None);
        Assert.NotNull(entry);

        Assert.Equal(userId, entry.UserId);
        Assert.Equal(itemId, entry.ItemId);
        Assert.True(entry.IsCompleted);
    }

    [Fact]
    public async Task OnEvent_UnsupportedItemType_DoesNotWriteEntry()
    {
        var repo = await CreateRepositoryAsync();
        var logger = CreateTracker();

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Audio, 10_000_000L, 0L);
        await logger.OnEvent(startArgs);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Audio, 10_000_000L, 5_000_000L, startArgs.Session.Id, startArgs.Session.PlaylistItemId);
        await logger.OnEvent(stopArgs);

        var entry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id), CancellationToken.None);
        Assert.Null(entry);
    }

    [Fact]
    public async Task OnEvent_SessionEnded_FinalizesIncompleteEntry()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker();

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 10_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(startArgs.Session.Id, startArgs.Session.PlaylistItemId, startArgs.MediaInfo.Id);
        var startEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(startEntry);

        // Simulate a session ending without a PlaybackStop event.
        startArgs.Session.UserId = userId;
        startArgs.Session.NowPlayingItem = startArgs.MediaInfo;
        startArgs.Session.PlayState.PositionTicks = 5_000_000L;
        startArgs.Session.LastPlaybackCheckIn = DateTime.UtcNow;

        var endedArgs = new SessionEndedEventArgs(startArgs.Session);
        await tracker.OnEvent(endedArgs);

        var completed = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(completed);
        Assert.True(completed.IsCompleted);
        Assert.NotNull(completed.EndTime);
    }

    private static PlaybackStartEventArgs CreatePlaybackStartEventArgs(Guid userId, Guid itemId, BaseItemKind kind, long runtimeTicks, long startPositionTicks)
    {
        var user = new Database.Implementations.Entities.User("User", "auth", "reset")
        {
            Id = userId
        };

        var media = new MediaBrowser.Model.Dto.BaseItemDto
        {
            Id = itemId,
            Name = "Item",
            Type = kind,
            MediaType = (MediaType)kind,
            RunTimeTicks = runtimeTicks
        };

        var mockSessionManager = new Mock<ISessionManager>().Object;
        var mockLogger = new Mock<ILogger<SessionInfo>>().Object;
        var session = new SessionInfo(mockSessionManager, mockLogger)
        {
            Id = Guid.NewGuid().ToString(),
            PlaylistItemId = Guid.NewGuid().ToString()
        };

        // Create a mock BaseItem that will be used by ImageCaptureService
        var mockItem = new Movie
        {
            Id = itemId,
            Name = "Item"
        };

        return new PlaybackStartEventArgs
        {
            Users = new List<Database.Implementations.Entities.User> { user },
            MediaInfo = media,
            PlaybackPositionTicks = startPositionTicks,
            Session = session,
            ClientName = "client",
            DeviceName = "device",
            Item = mockItem
        };
    }

    private static PlaybackStopEventArgs CreatePlaybackStopEventArgs(Guid userId, Guid itemId, BaseItemKind kind, long runtimeTicks, long positionTicks, string sessionId, string playlistItemId)
    {
        var user = new Database.Implementations.Entities.User("User", "auth", "reset")
        {
            Id = userId
        };

        var media = new MediaBrowser.Model.Dto.BaseItemDto
        {
            Id = itemId,
            Name = "Item",
            Type = kind,
            MediaType = (MediaType)kind,
            RunTimeTicks = runtimeTicks
        };

        var mockSessionManager = new Mock<ISessionManager>().Object;
        var mockLogger = new Mock<ILogger<SessionInfo>>().Object;
        var session = new SessionInfo(mockSessionManager, mockLogger)
        {
            Id = sessionId,
            PlaylistItemId = playlistItemId,
            LastPlaybackCheckIn = DateTime.UtcNow
        };

        // Create a mock BaseItem that will be used by ImageCaptureService
        var mockItem = new Movie
        {
            Id = itemId,
            Name = "Item"
        };

        return new PlaybackStopEventArgs
        {
            Users = [user],
            MediaInfo = media,
            PlaybackPositionTicks = positionTicks,
            Session = session,
            ClientName = "client",
            DeviceName = "device",
            Item = mockItem
        };
    }

    [Fact]
    public async Task OnEvent_StopBelowMinimumThreshold_DoesNotMarkCompleted()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Watch only 10% (below 20% threshold)
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 10_000_000L, startArgs.Session.Id, startArgs.Session.PlaylistItemId);
        await tracker.OnEvent(stopArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id);
        var completedEntry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.Null(completedEntry); // Should not be in completed

        var incompleteEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.Null(incompleteEntry); // Should be deleted when below threshold
    }

    [Fact]
    public async Task OnEvent_StopBetweenThresholds_MarksCompletedWithOriginalPosition()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Watch 50% (between 20% and 90%)
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 50_000_000L, startArgs.Session.Id, startArgs.Session.PlaylistItemId);
        await tracker.OnEvent(stopArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted);
        Assert.Equal(50_000_000L, entry.EndPositionTicks); // Original position, not normalized
    }

    [Fact]
    public async Task OnEvent_StopAboveMaximumThreshold_MarksCompletedAndNormalizesPosition()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Watch 95% (above 90% threshold)
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 95_000_000L, startArgs.Session.Id, startArgs.Session.PlaylistItemId);
        await tracker.OnEvent(stopArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted);
        Assert.Equal(100_000_000L, entry.EndPositionTicks); // Normalized to runtime
    }

    [Fact]
    public async Task OnEvent_StopWithNullRuntimeTicks_AlwaysMarksCompleted()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // null runtime (live TV or unknown duration)
        var user = new Database.Implementations.Entities.User("User", "auth", "reset") { Id = userId };
        var media = new MediaBrowser.Model.Dto.BaseItemDto
        {
            Id = itemId,
            Name = "Item",
            Type = BaseItemKind.Movie,
            MediaType = MediaType.Video,
            RunTimeTicks = null // No runtime
        };

        var mockSessionManager = new Mock<ISessionManager>().Object;
        var mockLogger = new Mock<ILogger<SessionInfo>>().Object;
        var session = new SessionInfo(mockSessionManager, mockLogger)
        {
            Id = Guid.NewGuid().ToString(),
            PlaylistItemId = Guid.NewGuid().ToString(),
            LastPlaybackCheckIn = DateTime.UtcNow
        };

        var mockItem = new Movie { Id = itemId, Name = "Item" };

        var stopArgs = new PlaybackStopEventArgs
        {
            Users = [user],
            MediaInfo = media,
            PlaybackPositionTicks = 5_000_000L,
            Session = session,
            ClientName = "client",
            DeviceName = "device",
            Item = mockItem
        };

        await tracker.OnEvent(stopArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted); // Should always mark completed for null runtime
    }

    [Fact]
    public async Task OnEvent_StopWithZeroConfigThresholds_AlwaysMarksCompleted()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 0, maxResumePct: 0);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Watch only 1% with zero config thresholds
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 1_000_000L, startArgs.Session.Id, startArgs.Session.PlaylistItemId);
        await tracker.OnEvent(stopArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(stopArgs.Session.Id, stopArgs.Session.PlaylistItemId, stopArgs.MediaInfo.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted); // Zero thresholds = any progress marks completed
        Assert.Equal(1_000_000L, entry.EndPositionTicks); // Not normalized (maxResumePct is 0)
    }

    [Fact]
    public async Task OnEvent_SessionEndedBelowMinimumThreshold_DoesNotMarkCompleted()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Start playback
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        // Session ends after watching only 10% (below threshold)
        startArgs.Session.UserId = userId;
        startArgs.Session.NowPlayingItem = startArgs.MediaInfo;
        startArgs.Session.PlayState.PositionTicks = 10_000_000L;
        startArgs.Session.LastPlaybackCheckIn = DateTime.UtcNow;

        var endedArgs = new SessionEndedEventArgs(startArgs.Session);
        await tracker.OnEvent(endedArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(startArgs.Session.Id, startArgs.Session.PlaylistItemId, startArgs.MediaInfo.Id);
        var completedEntry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.Null(completedEntry);

        var incompleteEntry = await repo.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.Null(incompleteEntry); // Should be deleted when below threshold
    }

    [Fact]
    public async Task OnEvent_SessionEndedAboveMaximumThreshold_MarksCompletedAndNormalizesPosition()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Start playback
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        // Session ends after watching 92% (above maximum threshold)
        startArgs.Session.UserId = userId;
        startArgs.Session.NowPlayingItem = startArgs.MediaInfo;
        startArgs.Session.PlayState.PositionTicks = 92_000_000L;
        startArgs.Session.LastPlaybackCheckIn = DateTime.UtcNow;

        var endedArgs = new SessionEndedEventArgs(startArgs.Session);
        await tracker.OnEvent(endedArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(startArgs.Session.Id, startArgs.Session.PlaylistItemId, startArgs.MediaInfo.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted);
        Assert.Equal(100_000_000L, entry.EndPositionTicks); // Normalized to runtime
    }

    [Fact]
    public async Task OnEvent_SessionEndedWithNullRuntimeTicks_AlwaysMarksCompleted()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Create a start event with null runtime
        var user = new Database.Implementations.Entities.User("User", "auth", "reset") { Id = userId };
        var media = new MediaBrowser.Model.Dto.BaseItemDto
        {
            Id = itemId,
            Name = "LiveTV",
            Type = BaseItemKind.Movie,
            MediaType = MediaType.Video,
            RunTimeTicks = null // No runtime (live TV or unknown)
        };

        var mockSessionManager = new Mock<ISessionManager>().Object;
        var mockLogger = new Mock<ILogger<SessionInfo>>().Object;
        var session = new SessionInfo(mockSessionManager, mockLogger)
        {
            Id = Guid.NewGuid().ToString(),
            PlaylistItemId = Guid.NewGuid().ToString(),
            LastPlaybackCheckIn = DateTime.UtcNow
        };

        var mockItem = new Movie { Id = itemId, Name = "LiveTV" };

        var startArgs = new PlaybackStartEventArgs
        {
            Users = [user],
            MediaInfo = media,
            PlaybackPositionTicks = 0L,
            Session = session,
            ClientName = "client",
            DeviceName = "device",
            Item = mockItem
        };

        await tracker.OnEvent(startArgs);

        // Session ends after watching some duration
        session.UserId = userId;
        session.NowPlayingItem = media;
        session.PlayState.PositionTicks = 5_000_000L;
        session.LastPlaybackCheckIn = DateTime.UtcNow;

        var endedArgs = new SessionEndedEventArgs(session);
        await tracker.OnEvent(endedArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(session.Id, session.PlaylistItemId, media.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted); // Should always mark completed for null runtime
    }

    [Fact]
    public async Task OnEvent_SessionEndedBetweenThresholds_MarksCompletedWithOriginalPosition()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 20, maxResumePct: 90);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Start playback
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        // Session ends after watching 50% (between thresholds)
        startArgs.Session.UserId = userId;
        startArgs.Session.NowPlayingItem = startArgs.MediaInfo;
        startArgs.Session.PlayState.PositionTicks = 50_000_000L;
        startArgs.Session.LastPlaybackCheckIn = DateTime.UtcNow;

        var endedArgs = new SessionEndedEventArgs(startArgs.Session);
        await tracker.OnEvent(endedArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(startArgs.Session.Id, startArgs.Session.PlaylistItemId, startArgs.MediaInfo.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted);
        Assert.Equal(50_000_000L, entry.EndPositionTicks); // Original position, not normalized
    }

    [Fact]
    public async Task OnEvent_SessionEndedWithZeroConfigThresholds_AlwaysMarksCompleted()
    {
        var repo = await CreateRepositoryAsync();
        var tracker = CreateTracker(minResumePct: 0, maxResumePct: 0);

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Start playback
        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 100_000_000L, 0L);
        await tracker.OnEvent(startArgs);

        // Session ends after watching only 1% with zero config thresholds
        startArgs.Session.UserId = userId;
        startArgs.Session.NowPlayingItem = startArgs.MediaInfo;
        startArgs.Session.PlayState.PositionTicks = 1_000_000L;
        startArgs.Session.LastPlaybackCheckIn = DateTime.UtcNow;

        var endedArgs = new SessionEndedEventArgs(startArgs.Session);
        await tracker.OnEvent(endedArgs);

        var playbackId = PlaybackEntry.GeneratePlaybackId(startArgs.Session.Id, startArgs.Session.PlaylistItemId, startArgs.MediaInfo.Id);
        var entry = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(entry);
        Assert.True(entry.IsCompleted); // Zero thresholds = any progress marks completed
        Assert.Equal(1_000_000L, entry.EndPositionTicks); // Not normalized (maxResumePct is 0)
    }
}
