using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public class PlaybackEntryRepositoryTests
{
    private static readonly DatabaseHelper DatabaseHelper;

    static PlaybackEntryRepositoryTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
        Directory.CreateDirectory(tempDir);
        DatabaseHelper = new DatabaseHelper(tempDir);
        DatabaseHelper.Initialize();
    }

    private static async Task CleanDatabaseAsync(PlaybackEntryRepository repository)
    {
        // Delete all entries by using a far future date
        await repository.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddYears(1000), CancellationToken.None);

        // Also manually clean incomplete entries (those with NULL EndTime)
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseHelper.ConnectionString);
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM PlaybackEntries WHERE EndTime IS NULL;";
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task GetActivitiesAsync_ReturnsMostRecentFirst_AndSupportsCursor()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;
        var older = CreateEntry(now.AddMinutes(-20), now.AddMinutes(-10), ContentType.Movie);
        var newer = CreateEntry(now.AddMinutes(-10), now, ContentType.Movie);

        await repository.AppendAsync(older, cancellationToken);
        await repository.AppendAsync(newer, cancellationToken);

        var (firstPage, lastId, lastEndUtc) = await repository.GetActivitiesAsync(1, null, null, false, cancellationToken);
        Assert.Single(firstPage);
        Assert.Equal(newer.EndTime, firstPage[0].EndTime);

        var (secondPage, _, _) = await repository.GetActivitiesAsync(10, lastId, lastEndUtc, false, cancellationToken);
        Assert.Single(secondPage);
        Assert.Equal(older.EndTime, secondPage[0].EndTime);
    }

    [Fact]
    public async Task GetActivitiesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var (entries, lastId, lastEndUtc) = await repository.GetActivitiesAsync(10, null, null, false, cancellationToken);

        Assert.Empty(entries);
        Assert.Null(lastId);
        Assert.Null(lastEndUtc);
    }

    [Fact]
    public async Task GetActivitiesAsync_FiltersOutOtherContentType()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTime.UtcNow;
        var movie = CreateEntry(now.AddMinutes(-30), now.AddMinutes(-20), ContentType.Movie);
        var episode = CreateEntry(now.AddMinutes(-20), now.AddMinutes(-10), ContentType.Episode);
        var other = CreateEntry(now.AddMinutes(-10), now, ContentType.Other);

        await repository.AppendAsync(movie, cancellationToken);
        await repository.AppendAsync(episode, cancellationToken);
        await repository.AppendAsync(other, cancellationToken);

        var (entries, _, _) = await repository.GetActivitiesAsync(10, null, null, false, cancellationToken);

        Assert.Equal(2, entries.Count);
        Assert.DoesNotContain(entries, e => e.ContentType == ContentType.Other);
        Assert.Contains(entries, e => e.PlaybackId == movie.PlaybackId);
        Assert.Contains(entries, e => e.PlaybackId == episode.PlaybackId);
    }

    [Fact]
    public async Task GetActivitiesAsync_ExcludesIncompleteWhenNotIncludeActive()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;
        var completed = CreateEntry(now.AddMinutes(-20), now.AddMinutes(-10), ContentType.Movie);
        var incomplete = CreateEntry(now.AddMinutes(-10), (DateTimeOffset?)null, ContentType.Movie);
        incomplete.IsCompleted = false;

        await repository.AppendAsync(completed, cancellationToken);
        await repository.AppendAsync(incomplete, cancellationToken);

        var (entries, _, _) = await repository.GetActivitiesAsync(10, null, null, false, cancellationToken);

        Assert.Single(entries);
        Assert.Equal(completed.PlaybackId, entries[0].PlaybackId);
        Assert.True(entries[0].IsCompleted);
    }

    [Fact]
    public async Task GetActivitiesAsync_IncludesIncompleteWhenIncludeActive()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;
        var completed = CreateEntry(now.AddMinutes(-30), now.AddMinutes(-20), ContentType.Movie);
        var incomplete = CreateEntry(now.AddMinutes(-10), (DateTimeOffset?)null, ContentType.Movie);
        incomplete.IsCompleted = false;

        await repository.AppendAsync(completed, cancellationToken);
        await repository.AppendAsync(incomplete, cancellationToken);

        var (entries, _, _) = await repository.GetActivitiesAsync(10, null, null, true, cancellationToken);

        Assert.Equal(2, entries.Count);

        // Incomplete entry should be first (most recent StartTime)
        Assert.Equal(incomplete.PlaybackId, entries[0].PlaybackId);
        Assert.False(entries[0].IsCompleted);
        Assert.Null(entries[0].EndTime);

        Assert.Equal(completed.PlaybackId, entries[1].PlaybackId);
        Assert.True(entries[1].IsCompleted);
    }

    [Fact]
    public async Task GetActivitiesAsync_OrdersByEndUtcDescThenIdDesc()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;
        var sameEndTime = now.AddMinutes(-10);

        // Create three entries with the same EndTime
        var entry1 = CreateEntry(now.AddMinutes(-30), sameEndTime, ContentType.Movie);
        var entry2 = CreateEntry(now.AddMinutes(-30), sameEndTime, ContentType.Movie);
        var entry3 = CreateEntry(now.AddMinutes(-30), sameEndTime, ContentType.Movie);

        await repository.AppendAsync(entry1, cancellationToken);
        await repository.AppendAsync(entry2, cancellationToken);
        await repository.AppendAsync(entry3, cancellationToken);

        var (entries, _, _) = await repository.GetActivitiesAsync(10, null, null, false, cancellationToken);

        Assert.Equal(3, entries.Count);
        // Should be ordered by Id DESC when EndTime is the same
        Assert.True(entries[0].Id > entries[1].Id);
        Assert.True(entries[1].Id > entries[2].Id);
    }

    [Fact]
    public async Task GetActivitiesAsync_RespectsLimitParameter()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < 10; i++)
        {
            var entry = CreateEntry(now.AddMinutes(-20 - i), now.AddMinutes(-10 - i), ContentType.Movie);
            await repository.AppendAsync(entry, cancellationToken);
        }

        var (entries, _, _) = await repository.GetActivitiesAsync(5, null, null, false, cancellationToken);

        Assert.Equal(5, entries.Count);
    }

    [Fact]
    public async Task GetActivitiesAsync_PaginationWorksAcrossMultiplePages()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;
        var allEntries = new List<PlaybackEntry>();

        for (int i = 0; i < 7; i++)
        {
            var entry = CreateEntry(now.AddMinutes(-30 - i * 2), now.AddMinutes(-20 - i * 2), ContentType.Movie);
            await repository.AppendAsync(entry, cancellationToken);
            allEntries.Add(entry);
        }

        // Page 1
        var (page1, lastId1, lastEndUtc1) = await repository.GetActivitiesAsync(3, null, null, false, cancellationToken);
        Assert.Equal(3, page1.Count);
        Assert.NotNull(lastId1);
        Assert.NotNull(lastEndUtc1);

        // Page 2
        var (page2, lastId2, lastEndUtc2) = await repository.GetActivitiesAsync(3, lastId1, lastEndUtc1, false, cancellationToken);
        Assert.Equal(3, page2.Count);
        Assert.NotNull(lastId2);
        Assert.NotNull(lastEndUtc2);

        // Page 3 (last page)
        var (page3, lastId3, lastEndUtc3) = await repository.GetActivitiesAsync(3, lastId2, lastEndUtc2, false, cancellationToken);
        Assert.Single(page3);
        Assert.NotNull(lastId3);
        Assert.NotNull(lastEndUtc3);

        // Verify no overlap between pages
        var allIds = page1.Concat(page2).Concat(page3).Select(e => e.PlaybackId).ToList();
        Assert.Equal(7, allIds.Distinct().Count());

        // Verify ordering across pages
        var allEndTimes = page1.Concat(page2).Concat(page3).Select(e => e.EndTime!.Value).ToList();
        Assert.Equal(allEndTimes, allEndTimes.OrderByDescending(t => t).ToList());
    }

    [Fact]
    public async Task GetActivitiesAsync_IncludeActive_OrdersByMostRecentActivity()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;

        // Old completed entry
        var oldCompleted = CreateEntry(now.AddMinutes(-60), now.AddMinutes(-50), ContentType.Movie);

        // Recent incomplete entry (active)
        var recentActive = CreateEntry(now.AddMinutes(-5), (DateTimeOffset?)null, ContentType.Movie);
        recentActive.IsCompleted = false;

        // Recent completed entry
        var recentCompleted = CreateEntry(now.AddMinutes(-20), now.AddMinutes(-10), ContentType.Movie);

        await repository.AppendAsync(oldCompleted, cancellationToken);
        await repository.AppendAsync(recentActive, cancellationToken);
        await repository.AppendAsync(recentCompleted, cancellationToken);

        var (entries, _, _) = await repository.GetActivitiesAsync(10, null, null, true, cancellationToken);

        Assert.Equal(3, entries.Count);

        // Order should be: recentActive (StartTime), recentCompleted (EndTime), oldCompleted (EndTime)
        Assert.Equal(recentActive.PlaybackId, entries[0].PlaybackId);
        Assert.False(entries[0].IsCompleted);

        Assert.Equal(recentCompleted.PlaybackId, entries[1].PlaybackId);
        Assert.True(entries[1].IsCompleted);

        Assert.Equal(oldCompleted.PlaybackId, entries[2].PlaybackId);
        Assert.True(entries[2].IsCompleted);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOnlyEntriesBeforeCutoff()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await CleanDatabaseAsync(repository);

        var now = DateTimeOffset.UtcNow;
        var older = CreateEntry(now.AddDays(-10), now.AddDays(-9), ContentType.Movie);
        var newer = CreateEntry(now.AddDays(-2), now.AddDays(-1), ContentType.Movie);

        await repository.AppendAsync(older, cancellationToken);
        await repository.AppendAsync(newer, cancellationToken);

        var cutoff = newer.EndTime!.Value.AddDays(-2);
        var removed = await repository.DeleteOlderThanAsync(cutoff, cancellationToken);
        Assert.Equal(1, removed);

        var newer_entry = await repository.GetRecentlyCompletedByPlaybackIdAsync(newer.PlaybackId, cancellationToken);
        Assert.NotNull(newer_entry);
        Assert.Equal(newer.EndTime, newer_entry.EndTime);

        var older_entry = await repository.GetRecentlyCompletedByPlaybackIdAsync(older.PlaybackId, cancellationToken);
        Assert.Null(older_entry);
    }

    private static PlaybackEntry CreateEntry(DateTimeOffset startTime, DateTimeOffset? endUtc, ContentType contentType = ContentType.Movie)
    {
        return new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            ContentType = contentType,
            Title = "Item",
            UserId = Guid.NewGuid(),
            UserName = "User",
            ClientName = "TestClient",
            DeviceName = "TestDevice",
            StartTime = startTime,
            EndTime = endUtc,
            IsCompleted = endUtc.HasValue
        };
    }
}
