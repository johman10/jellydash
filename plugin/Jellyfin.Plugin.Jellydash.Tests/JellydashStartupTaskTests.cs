using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public sealed class JellydashStartupTaskTests
{
    private readonly DatabaseHelper _databaseHelper;

    public JellydashStartupTaskTests()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
        Directory.CreateDirectory(tempRoot);
        _databaseHelper = new DatabaseHelper(Path.Combine(tempRoot, "startup_task.db"));
        _databaseHelper.Initialize();
    }

    private static PlaybackEntry CreateEntry(DateTimeOffset startTime, DateTimeOffset? endTime, ContentType contentType)
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            ContentType = contentType,
            Title = "Test Title",
            UserId = Guid.NewGuid(),
            UserName = "TestUser",
            StartTime = startTime,
            EndTime = endTime,
            UpdatedAt = DateTimeOffset.UtcNow,
            StartPositionTicks = 0,
            IsCompleted = endTime.HasValue
        };
        return entry;
    }

    private async Task CleanDatabaseAsync(PlaybackEntryRepository repository)
    {
        var entries = await repository.GetAllIncompleteEntriesAsync(CancellationToken.None);
        foreach (var entry in entries)
        {
            if (entry.Id > 0)
            {
                await repository.DeleteByIdAsync(entry.Id, CancellationToken.None);
            }
        }
    }

    private Mock<IServerConfigurationManager> CreateMockConfigurationManager(int? minResumePct = null)
    {
        var mockConfig = new Mock<IServerConfigurationManager>();
        var configuration = new MediaBrowser.Model.Configuration.ServerConfiguration
        {
            MinResumePct = minResumePct ?? 10
        };
        mockConfig.Setup(c => c.Configuration).Returns(configuration);
        return mockConfig;
    }

    [Fact]
    public async Task StartAsync_CompletesAndDeletesBasedOnThreshold()
    {
        var repository = new PlaybackEntryRepository(_databaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;

        // Create an incomplete entry that meets the 10% minimum threshold (50% watched)
        var incompleteEntry1 = CreateEntry(now.AddMinutes(-30), null, ContentType.Movie);
        incompleteEntry1.StartPositionTicks = 0;
        incompleteEntry1.EndPositionTicks = 5000;
        incompleteEntry1.RuntimeTicks = 10000; // 50% watched
        incompleteEntry1.UpdatedAt = now.AddMinutes(-25);

        // Create an incomplete entry that doesn't meet the threshold (5% watched)
        var incompleteEntry2 = CreateEntry(now.AddMinutes(-20), null, ContentType.Episode);
        incompleteEntry2.StartPositionTicks = 0;
        incompleteEntry2.EndPositionTicks = 500;
        incompleteEntry2.RuntimeTicks = 10000; // 5% watched
        incompleteEntry2.UpdatedAt = now.AddMinutes(-18);

        // Create a completed entry to verify it's not affected
        var completedEntry = CreateEntry(now.AddMinutes(-10), now, ContentType.Movie);

        await repository.AppendAsync(incompleteEntry1, cancellationToken);
        await repository.AppendAsync(incompleteEntry2, cancellationToken);
        await repository.AppendAsync(completedEntry, cancellationToken);

        // Create and start the task
        var mockConfig = CreateMockConfigurationManager(10);
        var mockLogger = new Mock<ILogger<JellydashStartupTask>>();
        var task = new JellydashStartupTask(mockLogger.Object, repository, mockConfig.Object);

        // Act
        await task.StartAsync(cancellationToken);

        // Verify entry1 is now completed with UpdatedAt used as EndTime
        var entry1After = await repository.GetRecentlyCompletedByPlaybackIdAsync(incompleteEntry1.PlaybackId, cancellationToken);
        Assert.NotNull(entry1After);
        Assert.True(entry1After.IsCompleted);
        Assert.False(entry1After.IsPaused);
        Assert.NotNull(entry1After.EndTime);
        Assert.Equal(10000, entry1After.EndPositionTicks); // Normalized to RuntimeTicks since 50% exceeds 10% threshold

        // Verify entry2 was deleted
        var entry2After = await repository.GetRecentlyCompletedByPlaybackIdAsync(incompleteEntry2.PlaybackId, cancellationToken);
        Assert.Null(entry2After);
        var entry2AfterIncomplete = await repository.GetRecentlyIncompletedByPlaybackIdAsync(incompleteEntry2.PlaybackId, cancellationToken);
        Assert.Null(entry2AfterIncomplete);

        // Verify completed entry is unchanged
        var completedAfter = await repository.GetRecentlyCompletedByPlaybackIdAsync(completedEntry.PlaybackId, cancellationToken);
        Assert.NotNull(completedAfter);
        Assert.Equal(completedEntry.EndTime, completedAfter.EndTime);
    }

    [Fact]
    public async Task StartAsync_NoIncompleteEntries_CompletesSuccessfully()
    {
        var repository = new PlaybackEntryRepository(_databaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database
        await CleanDatabaseAsync(repository);

        // Create only completed entries
        var entry1 = CreateEntry(DateTimeOffset.UtcNow.AddMinutes(-20), DateTimeOffset.UtcNow.AddMinutes(-10), ContentType.Movie);
        var entry2 = CreateEntry(DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow, ContentType.Episode);

        await repository.AppendAsync(entry1, cancellationToken);
        await repository.AppendAsync(entry2, cancellationToken);

        // Create and start the task
        var mockConfig = CreateMockConfigurationManager(10);
        var mockLogger = new Mock<ILogger<JellydashStartupTask>>();
        var task = new JellydashStartupTask(mockLogger.Object, repository, mockConfig.Object);

        // Act - should complete without errors
        await task.StartAsync(cancellationToken);

        // Verify entries are still completed
        var entry1After = await repository.GetRecentlyCompletedByPlaybackIdAsync(entry1.PlaybackId, cancellationToken);
        Assert.NotNull(entry1After);
        var entry2After = await repository.GetRecentlyCompletedByPlaybackIdAsync(entry2.PlaybackId, cancellationToken);
        Assert.NotNull(entry2After);
    }

    [Fact]
    public async Task StartAsync_NullEndPositionTicks_GetsDeletedBelowThreshold()
    {
        var repository = new PlaybackEntryRepository(_databaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;

        // Create an incomplete entry with null EndPositionTicks
        // When EndPositionTicks is null, it gets set to StartPositionTicks, resulting in 0% watched
        var incompleteEntry = CreateEntry(now.AddMinutes(-30), null, ContentType.Movie);
        incompleteEntry.StartPositionTicks = 1000;
        incompleteEntry.EndPositionTicks = null; // Will be set to StartPositionTicks (1000)
        incompleteEntry.RuntimeTicks = 10000; // Results in 0% watched (1000 - 1000 = 0)
        incompleteEntry.UpdatedAt = now.AddMinutes(-25);

        await repository.AppendAsync(incompleteEntry, cancellationToken);

        // Create and start the task with 10% threshold
        var mockConfig = CreateMockConfigurationManager(10);
        var mockLogger = new Mock<ILogger<JellydashStartupTask>>();
        var task = new JellydashStartupTask(mockLogger.Object, repository, mockConfig.Object);

        // Act
        await task.StartAsync(cancellationToken);

        // Verify entry was deleted (0% watched doesn't meet 10% threshold)
        var entryAfter = await repository.GetRecentlyCompletedByPlaybackIdAsync(incompleteEntry.PlaybackId, cancellationToken);
        Assert.Null(entryAfter);
        var entryAfterIncomplete = await repository.GetRecentlyIncompletedByPlaybackIdAsync(incompleteEntry.PlaybackId, cancellationToken);
        Assert.Null(entryAfterIncomplete);
    }

    [Fact]
    public async Task StartAsync_NullRuntimeTicks_AlwaysCompletes()
    {
        var repository = new PlaybackEntryRepository(_databaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;

        // Create an incomplete entry with null RuntimeTicks (e.g., live TV)
        var incompleteEntry = CreateEntry(now.AddMinutes(-30), null, ContentType.Movie);
        incompleteEntry.StartPositionTicks = 0;
        incompleteEntry.EndPositionTicks = 100;
        incompleteEntry.RuntimeTicks = null; // Unknown duration - should always complete
        incompleteEntry.UpdatedAt = now.AddMinutes(-25);

        await repository.AppendAsync(incompleteEntry, cancellationToken);

        // Create and start the task with high threshold
        var mockConfig = CreateMockConfigurationManager(90); // High threshold
        var mockLogger = new Mock<ILogger<JellydashStartupTask>>();
        var task = new JellydashStartupTask(mockLogger.Object, repository, mockConfig.Object);

        // Act
        await task.StartAsync(cancellationToken);

        // Verify entry is completed despite high threshold (null runtime always completes)
        var entryAfter = await repository.GetRecentlyCompletedByPlaybackIdAsync(incompleteEntry.PlaybackId, cancellationToken);
        Assert.NotNull(entryAfter);
        Assert.True(entryAfter.IsCompleted);
    }

    [Fact]
    public async Task StartAsync_ZeroMinResumePct_CompletesAllIncomplete()
    {
        var repository = new PlaybackEntryRepository(_databaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;

        // Create an incomplete entry with very low watch percentage (1%)
        var incompleteEntry = CreateEntry(now.AddMinutes(-30), null, ContentType.Movie);
        incompleteEntry.StartPositionTicks = 0;
        incompleteEntry.EndPositionTicks = 100;
        incompleteEntry.RuntimeTicks = 10000; // 1% watched
        incompleteEntry.UpdatedAt = now.AddMinutes(-25);

        await repository.AppendAsync(incompleteEntry, cancellationToken);

        // Create and start the task with zero threshold
        var mockConfig = CreateMockConfigurationManager(0);
        var mockLogger = new Mock<ILogger<JellydashStartupTask>>();
        var task = new JellydashStartupTask(mockLogger.Object, repository, mockConfig.Object);

        // Act
        await task.StartAsync(cancellationToken);

        // Verify entry is completed (zero threshold marks everything as completed)
        var entryAfter = await repository.GetRecentlyCompletedByPlaybackIdAsync(incompleteEntry.PlaybackId, cancellationToken);
        Assert.NotNull(entryAfter);
        Assert.True(entryAfter.IsCompleted);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var repository = new PlaybackEntryRepository(_databaseHelper);
        var mockConfig = CreateMockConfigurationManager();
        var mockLogger = new Mock<ILogger<JellydashStartupTask>>();
        var task = new JellydashStartupTask(mockLogger.Object, repository, mockConfig.Object);

        // Act - StopAsync should complete without errors
        await task.StopAsync(CancellationToken.None);

        // Assert - no exception thrown
        Assert.True(true);
    }
}
