using Jellyfin.Plugin.Jellydash.Controllers;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public sealed class JellydashControllerTests
{
    private static readonly DatabaseHelper DatabaseHelper;

    static JellydashControllerTests()
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

    [Fact]
    public async Task GetActivity_InvalidCursor_ReturnsBadRequest()
    {
        var repo = await CreateRepositoryAsync();
        var controller = new JellydashController(repo);

        var result = await controller.GetHistory(null, "not-a-valid-cursor", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid cursor.", badRequest.Value);
    }

    [Fact]
    public async Task GetActivity_RespectsDefaultLimitAndOrdering()
    {
        var repo = await CreateRepositoryAsync();

        // Insert more than the default limit (20) with increasing EndUtc.
        var now = DateTime.UtcNow;
        for (int i = 0; i < 25; i++)
        {
            var endUtc = now.AddMinutes(i);
            var startUtc = endUtc.AddMinutes(-10);
            var entry = new PlaybackEntry
            {
                PlaybackId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ContentKind = ContentKind.Movie,
                Title = $"Item-{i}",
                UserId = Guid.NewGuid(),
                UserName = "User",
                ClientName = "TestClient",
                DeviceName = "TestDevice",
                StartUtc = startUtc,
                EndUtc = endUtc,
                IsCompleted = true
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        var controller = new JellydashController(repo);

        var result = await controller.GetHistory(null, null, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        var items = GetItems(json);
        var nextCursor = GetNextCursor(json);

        Assert.Equal(20, items.Count);
        var orderedByEnd = items.OrderByDescending(e => e.Timing.EndUtc).ToList();
        Assert.True(items.Select(i => i.Timing.EndUtc).SequenceEqual(orderedByEnd.Select(i => i.Timing.EndUtc)));
        Assert.False(string.IsNullOrEmpty(nextCursor));
    }

    [Fact]
    public async Task GetActivity_UsesCursorForPaging()
    {
        var repo = await CreateRepositoryAsync();

        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            var endUtc = now.AddMinutes(i);
            var startUtc = endUtc.AddMinutes(-10);
            var entry = new PlaybackEntry
            {
                PlaybackId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ContentKind = ContentKind.Movie,
                Title = $"Item-{i}",
                UserId = Guid.NewGuid(),
                UserName = "User",
                ClientName = "TestClient",
                DeviceName = "TestDevice",
                StartUtc = startUtc,
                EndUtc = endUtc,
                IsCompleted = true
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        var controller = new JellydashController(repo);

        // First page with explicit small limit.
        var firstResult = await controller.GetHistory(2, null, CancellationToken.None);
        var firstJson = Assert.IsType<JsonResult>(firstResult);
        var firstItems = GetItems(firstJson);
        var firstCursor = GetNextCursor(firstJson);

        Assert.Equal(2, firstItems.Count);
        Assert.NotNull(firstCursor);

        // Second page using cursor.
        var secondResult = await controller.GetHistory(2, firstCursor, CancellationToken.None);
        var secondJson = Assert.IsType<JsonResult>(secondResult);
        var secondItems = GetItems(secondJson);
        var secondCursor = GetNextCursor(secondJson);

        Assert.Equal(2, secondItems.Count);
        Assert.NotNull(secondCursor);
        Assert.NotEqual(firstCursor, secondCursor);

        // Ensure no overlap between pages and overall ordering by EndUtc.
        Assert.Empty(firstItems.Select(i => i.Timing.EndUtc).Intersect(secondItems.Select(i => i.Timing.EndUtc)));
        var allEndTimes = firstItems.Concat(secondItems).Select(i => i.Timing.EndUtc).ToList();
        Assert.True(allEndTimes.SequenceEqual(allEndTimes.OrderByDescending(t => t)));

        // There should still be at least one more item remaining.
        Assert.False(string.IsNullOrEmpty(secondCursor));
    }

    private static List<PlaybackEntryDto> GetItems(JsonResult json)
    {
        var value = json.Value ?? throw new InvalidOperationException("Result value is null.");
        var response = Assert.IsType<HistoryResponse>(value);
        return response.Items.ToList();
    }

    private static string? GetNextCursor(JsonResult json)
    {
        var value = json.Value ?? throw new InvalidOperationException("Result value is null.");
        var response = Assert.IsType<HistoryResponse>(value);
        return response.NextCursor;
    }
}
