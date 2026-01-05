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

    [Fact]
    public async Task GetPageAsync_ReturnsMostRecentFirst_AndSupportsCursor()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await repository.DeleteOlderThanAsync(DateTime.UtcNow.AddYears(1000), cancellationToken);

        var now = DateTime.UtcNow;
        var older = CreateEntry(now.AddMinutes(-20), now.AddMinutes(-10));
        var newer = CreateEntry(now.AddMinutes(-10), now);

        await repository.AppendAsync(older, cancellationToken);
        await repository.AppendAsync(newer, cancellationToken);

        var (firstPage, lastId, lastEndUtc) = await repository.GetPageAsync(1, null, null, cancellationToken);
        Assert.Single(firstPage);
        Assert.Equal(newer.EndUtc, firstPage[0].EndUtc);

        var (secondPage, _, _) = await repository.GetPageAsync(10, lastId, lastEndUtc, cancellationToken);
        Assert.Single(secondPage);
        Assert.Equal(older.EndUtc, secondPage[0].EndUtc);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOnlyEntriesBeforeCutoff()
    {
        var repository = new PlaybackEntryRepository(DatabaseHelper);
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await repository.DeleteOlderThanAsync(DateTime.UtcNow.AddYears(1000), cancellationToken);

        var now = DateTime.UtcNow;
        var older = CreateEntry(now.AddDays(-10), now.AddDays(-9));
        var newer = CreateEntry(now.AddDays(-2), now.AddDays(-1));

        await repository.AppendAsync(older, cancellationToken);
        await repository.AppendAsync(newer, cancellationToken);

        var cutoff = newer.EndUtc!.Value.AddDays(-2);
        var removed = await repository.DeleteOlderThanAsync(cutoff, cancellationToken);
        Assert.Equal(1, removed);

        var newer_entry = await repository.GetRecentlyCompletedByPlaybackIdAsync(newer.PlaybackId, cancellationToken);
        Assert.NotNull(newer_entry);
        Assert.Equal(newer.EndUtc, newer_entry.EndUtc);

        var older_entry = await repository.GetRecentlyCompletedByPlaybackIdAsync(older.PlaybackId, cancellationToken);
        Assert.Null(older_entry);
    }

    private static PlaybackEntry CreateEntry(DateTime startUtc, DateTime endUtc)
    {
        return new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            ContentKind = ContentKind.Movie,
            Title = "Item",
            UserId = Guid.NewGuid(),
            UserName = "User",
            ClientName = "TestClient",
            DeviceName = "TestDevice",
            StartUtc = startUtc,
            EndUtc = endUtc,
            IsCompleted = true
        };
    }
}
