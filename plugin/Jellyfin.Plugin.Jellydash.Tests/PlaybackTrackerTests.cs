using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellydash.Events;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        await repo.DeleteOlderThanAsync(DateTime.UtcNow.AddYears(1000), CancellationToken.None);
        return repo;
    }

    private static PlaybackTracker CreateTracker()
    {
        var logger = Mock.Of<ILogger<PlaybackTracker>>();
        return new PlaybackTracker(logger, DatabaseHelper);
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

        var endedArgs = new SessionEndedEventArgs(startArgs.Session);
        await tracker.OnEvent(endedArgs);

        var completed = await repo.GetRecentlyCompletedByPlaybackIdAsync(playbackId, CancellationToken.None);
        Assert.NotNull(completed);
        Assert.True(completed.IsCompleted);
        Assert.NotNull(completed.EndUtc);
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


        return new PlaybackStartEventArgs
        {
            Users = new List<Database.Implementations.Entities.User> { user },
            MediaInfo = media,
            PlaybackPositionTicks = startPositionTicks,
            Session = session,
            ClientName = "client",
            DeviceName = "device"
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
            PlaylistItemId = playlistItemId
        };


        return new PlaybackStopEventArgs
        {
            Users = [user],
            MediaInfo = media,
            PlaybackPositionTicks = positionTicks,
            Session = session,
            ClientName = "client",
            DeviceName = "device"
        };
    }
}
