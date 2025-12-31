using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellydash.Events;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.Jellydash.Tests;

public sealed class PlaybackHistoryLoggerTests
{
    private static readonly string DatabasePath;

    static PlaybackHistoryLoggerTests()
    {
        if (string.IsNullOrEmpty(HistoryRepository.DatabasePathOverride))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
            Directory.CreateDirectory(tempDir);
            DatabasePath = Path.Combine(tempDir, "history_playback.db");
            HistoryRepository.DatabasePathOverride = DatabasePath;
        }
        else
        {
            DatabasePath = HistoryRepository.DatabasePathOverride;
        }
    }

    private static HistoryRepository CreateRepository()
    {
        var repo = new HistoryRepository();
        repo.DeleteOlderThanAsync(DateTime.UtcNow.AddYears(1000), CancellationToken.None).GetAwaiter().GetResult();
        return repo;
    }

    private static PlaybackHistoryLogger CreateLogger()
    {
        var logger = Mock.Of<ILogger<PlaybackHistoryLogger>>();
        return new PlaybackHistoryLogger(logger);
    }

    [Fact]
    public async Task OnEvent_StartAndStop_WritesHistoryEntry()
    {
        var repo = CreateRepository();
        var logger = CreateLogger();

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Movie, 10_000_000L, 0L);
        await logger.OnEvent(startArgs);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 10_000_000L, 5_000_000L, startArgs.PlaySessionId);
        await logger.OnEvent(stopArgs);

        var entries = await repo.GetRecentAsync(DateTime.MinValue, CancellationToken.None);
        Assert.Single(entries);
        var entry = entries[0];

        Assert.Equal(userId, entry.UserId);
        Assert.Equal(itemId, entry.ItemId);
        Assert.Equal(0, entry.StartPercentage);
        Assert.InRange(entry.EndPercentage, 49.0, 51.0);
    }

    [Fact]
    public async Task OnEvent_StopWithoutStart_StillWritesEntry()
    {
        var repo = CreateRepository();
        var logger = CreateLogger();

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Movie, 10_000_000L, 2_500_000L, playSessionId: "session-1");
        await logger.OnEvent(stopArgs);

        var entries = await repo.GetRecentAsync(DateTime.MinValue, CancellationToken.None);
        Assert.Single(entries);
        var entry = entries[0];

        Assert.Equal(userId, entry.UserId);
        Assert.Equal(itemId, entry.ItemId);
        Assert.InRange(entry.StartPercentage, 24.0, 26.0);
        Assert.InRange(entry.EndPercentage, 24.0, 26.0);
    }

    [Fact]
    public async Task OnEvent_UnsupportedItemType_DoesNotWriteEntry()
    {
        var repo = CreateRepository();
        var logger = CreateLogger();

        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var startArgs = CreatePlaybackStartEventArgs(userId, itemId, BaseItemKind.Audio, 10_000_000L, 0L);
        await logger.OnEvent(startArgs);

        var stopArgs = CreatePlaybackStopEventArgs(userId, itemId, BaseItemKind.Audio, 10_000_000L, 5_000_000L, startArgs.PlaySessionId);
        await logger.OnEvent(stopArgs);

        var entries = await repo.GetRecentAsync(DateTime.MinValue, CancellationToken.None);
        Assert.Empty(entries);
    }

    private static PlaybackStartEventArgs CreatePlaybackStartEventArgs(Guid userId, Guid itemId, BaseItemKind kind, long runtimeTicks, long startPositionTicks)
    {
        var user = new Jellyfin.Database.Implementations.Entities.User("User", "auth", "reset")
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


        return new PlaybackStartEventArgs
        {
            Users = new List<Jellyfin.Database.Implementations.Entities.User> { user },
            MediaInfo = media,
            PlaybackPositionTicks = startPositionTicks,
            PlaySessionId = "session-1",
            ClientName = "client",
            DeviceName = "device"
        };
    }

    private static PlaybackStopEventArgs CreatePlaybackStopEventArgs(Guid userId, Guid itemId, BaseItemKind kind, long runtimeTicks, long positionTicks, string? playSessionId)
    {
        var user = new Jellyfin.Database.Implementations.Entities.User("User", "auth", "reset")
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


        return new PlaybackStopEventArgs
        {
            Users = new List<Jellyfin.Database.Implementations.Entities.User> { user },
            MediaInfo = media,
            PlaybackPositionTicks = positionTicks,
            PlaySessionId = playSessionId,
            ClientName = "client",
            DeviceName = "device"
        };
    }
}
