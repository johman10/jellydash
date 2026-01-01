using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using Xunit;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public class HistoryRepositoryTests
{
    private static readonly string DatabasePath;

    static HistoryRepositoryTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
        Directory.CreateDirectory(tempDir);
        DatabasePath = Path.Combine(tempDir, "history.db");
        HistoryRepository.DatabasePathOverride = DatabasePath;
    }

    [Fact]
    public async Task GetPageAsync_ReturnsMostRecentFirst_AndSupportsCursor()
    {
        var repository = new HistoryRepository();
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
        var repository = new HistoryRepository();
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await repository.DeleteOlderThanAsync(DateTime.UtcNow.AddYears(1000), cancellationToken);

        var now = DateTime.UtcNow;
        var older = CreateEntry(now.AddDays(-10), now.AddDays(-9));
        var newer = CreateEntry(now.AddDays(-2), now.AddDays(-1));

        await repository.AppendAsync(older, cancellationToken);
        await repository.AppendAsync(newer, cancellationToken);

        var cutoff = newer.EndUtc.AddHours(-1);
        var removed = await repository.DeleteOlderThanAsync(cutoff, cancellationToken);
        Assert.Equal(1, removed);

        var remaining = await repository.GetRecentAsync(DateTime.MinValue, cancellationToken);
        Assert.Single(remaining);
        Assert.Equal(newer.EndUtc, remaining[0].EndUtc);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsEntriesOnOrAfterCutoff()
    {
        var repository = new HistoryRepository();
        var cancellationToken = CancellationToken.None;

        // Ensure a clean database.
        await repository.DeleteOlderThanAsync(DateTime.UtcNow.AddYears(1000), cancellationToken);

        var now = DateTime.UtcNow;
        var older = CreateEntry(now.AddHours(-3), now.AddHours(-2));
        var cutoffEntry = CreateEntry(now.AddHours(-2), now.AddHours(-1));
        var newer = CreateEntry(now.AddHours(-1), now);

        await repository.AppendAsync(older, cancellationToken);
        await repository.AppendAsync(cutoffEntry, cancellationToken);
        await repository.AppendAsync(newer, cancellationToken);

        var cutoff = cutoffEntry.EndUtc;
        var recent = await repository.GetRecentAsync(cutoff, cancellationToken);

        Assert.Equal(2, recent.Count);
        Assert.True(recent.All(e => e.EndUtc >= cutoff));
    }

    private static HistoryEntry CreateEntry(DateTime startUtc, DateTime endUtc)
    {
        return new HistoryEntry
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
    }
}
