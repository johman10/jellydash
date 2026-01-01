using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Controllers;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public sealed class JellydashControllerTests
{
    private static readonly string DatabasePath;

    static JellydashControllerTests()
    {
        if (string.IsNullOrEmpty(HistoryRepository.DatabasePathOverride))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "JellydashPluginTests");
            Directory.CreateDirectory(tempDir);
            DatabasePath = Path.Combine(tempDir, "history_controller.db");
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

    [Fact]
    public async Task GetHistory_InvalidCursor_ReturnsBadRequest()
    {
        var repo = CreateRepository();
        var controller = new JellydashController(repo);

        var result = await controller.GetHistory(null, "not-a-valid-cursor", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid cursor.", badRequest.Value);
    }

    [Fact]
    public async Task GetHistory_RespectsDefaultLimitAndOrdering()
    {
        var repo = CreateRepository();

        // Insert more than the default limit (20) with increasing EndUtc.
        var now = DateTime.UtcNow;
        for (int i = 0; i < 25; i++)
        {
            var endUtc = now.AddMinutes(i);
            var startUtc = endUtc.AddMinutes(-10);
            var entry = new HistoryEntry
            {
                UserId = Guid.NewGuid(),
                UserName = "User",
                ItemId = Guid.NewGuid(),
                MediaType = "Movie",
                ItemName = $"Item-{i}",
                StartUtc = startUtc,
                EndUtc = endUtc,
                StartPercentage = 0,
                EndPercentage = 100
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        var controller = new JellydashController(repo);

        var result = await controller.GetHistory(null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
    var items = GetItems(ok);
    var nextCursor = GetNextCursor(ok);

        Assert.Equal(20, items.Count);
        Assert.True(items.SequenceEqual(items.OrderByDescending(e => e.EndUtc)));
        Assert.False(string.IsNullOrEmpty(nextCursor));
    }

    [Fact]
    public async Task GetHistory_UsesCursorForPaging()
    {
        var repo = CreateRepository();

        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            var endUtc = now.AddMinutes(i);
            var startUtc = endUtc.AddMinutes(-10);
            var entry = new HistoryEntry
            {
                UserId = Guid.NewGuid(),
                UserName = "User",
                ItemId = Guid.NewGuid(),
                MediaType = "Movie",
                ItemName = $"Item-{i}",
                StartUtc = startUtc,
                EndUtc = endUtc,
                StartPercentage = 0,
                EndPercentage = 100
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        var controller = new JellydashController(repo);

        // First page with explicit small limit.
        var firstResult = await controller.GetHistory(2, null, CancellationToken.None);
        var firstOk = Assert.IsType<OkObjectResult>(firstResult);
    var firstItems = GetItems(firstOk);
    var firstCursor = GetNextCursor(firstOk);

        Assert.Equal(2, firstItems.Count);
        Assert.False(string.IsNullOrEmpty(firstCursor));

        // Second page using cursor.
        var secondResult = await controller.GetHistory(2, firstCursor, CancellationToken.None);
        var secondOk = Assert.IsType<OkObjectResult>(secondResult);
        var secondItems = GetItems(secondOk);
        var secondCursor = GetNextCursor(secondOk);

        Assert.Equal(2, secondItems.Count);

        // Ensure no overlap between pages and overall ordering by EndUtc.
        Assert.Empty(firstItems.Select(i => i.EndUtc).Intersect(secondItems.Select(i => i.EndUtc)));
        var allEndTimes = firstItems.Concat(secondItems).Select(i => i.EndUtc).ToList();
        Assert.True(allEndTimes.SequenceEqual(allEndTimes.OrderByDescending(t => t)));

        // There should still be at least one more item remaining.
        Assert.False(string.IsNullOrEmpty(secondCursor));
    }

    private static List<HistoryEntry> GetItems(OkObjectResult ok)
    {
        var value = ok.Value ?? throw new InvalidOperationException("Result value is null.");
        var itemsProperty = value.GetType().GetProperty("items");
        Assert.NotNull(itemsProperty);
        var itemsObj = itemsProperty!.GetValue(value);
        var itemsEnumerable = Assert.IsAssignableFrom<IEnumerable<HistoryEntry>>(itemsObj);
        return itemsEnumerable.ToList();
    }

    private static string? GetNextCursor(OkObjectResult ok)
    {
        var value = ok.Value ?? throw new InvalidOperationException("Result value is null.");
        var cursorProperty = value.GetType().GetProperty("nextCursor");
        Assert.NotNull(cursorProperty);
        return (string?)cursorProperty!.GetValue(value);
    }
}
