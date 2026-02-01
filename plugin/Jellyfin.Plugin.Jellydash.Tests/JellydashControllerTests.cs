using Jellyfin.Plugin.Jellydash.Controllers;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

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
        await repo.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddYears(1000), CancellationToken.None);
        return repo;
    }

    private static ImageCaptureService CreateMockImageCaptureService()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "JellydashControllerTests");
        return new ImageCaptureService(
            new Mock<IImageProcessor>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<ILogger<ImageCaptureService>>().Object,
            tempPath);
    }

    [Fact]
    public async Task GetActivity_InvalidCursor_ReturnsBadRequest()
    {
        var repo = await CreateRepositoryAsync();
        var imageCaptureService = CreateMockImageCaptureService();
        var controller = new JellydashController(repo, imageCaptureService);

        var result = await controller.GetActivity(null, "not-a-valid-cursor", false, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid cursor.", badRequest.Value);
    }

    [Fact]
    public async Task GetActivity_RespectsDefaultLimitAndOrdering()
    {
        var repo = await CreateRepositoryAsync();

        // Insert more than the default limit (20) with increasing EndTime.
        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < 25; i++)
        {
            var endTime = now.AddMinutes(i);
            var startTime = endTime.AddMinutes(-10);
            var entry = new PlaybackEntry
            {
                PlaybackId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ContentType = ContentType.Movie,
                Title = $"Item-{i}",
                UserId = Guid.NewGuid(),
                UserName = "User",
                ClientName = "TestClient",
                DeviceName = "TestDevice",
                StartTime = startTime,
                EndTime = endTime,
                IsCompleted = true
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        var imageCaptureService = CreateMockImageCaptureService();
        var controller = new JellydashController(repo, imageCaptureService);

        var result = await controller.GetActivity(null, null, false, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        var items = GetItems(json);
        var nextCursor = GetNextCursor(json);

        Assert.Equal(20, items.Count);
        Assert.All(items, item => Assert.True(item.IsCompleted));
        var orderedByEnd = items.OrderByDescending(e => e.Timing.EndTime).ToList();
        Assert.True(items.Select(i => i.Timing.EndTime).SequenceEqual(orderedByEnd.Select(i => i.Timing.EndTime)));
        Assert.False(string.IsNullOrEmpty(nextCursor));
    }

    [Fact]
    public async Task GetActivity_UsesCursorForPaging()
    {
        var repo = await CreateRepositoryAsync();

        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            var endTime = now.AddMinutes(i);
            var startTime = endTime.AddMinutes(-10);
            var entry = new PlaybackEntry
            {
                PlaybackId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ContentType = ContentType.Movie,
                Title = $"Item-{i}",
                UserId = Guid.NewGuid(),
                UserName = "User",
                ClientName = "TestClient",
                DeviceName = "TestDevice",
                StartTime = startTime,
                EndTime = endTime,
                IsCompleted = true
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        var imageCaptureService = CreateMockImageCaptureService();
        var controller = new JellydashController(repo, imageCaptureService);

        // First page with explicit small limit.
        var firstResult = await controller.GetActivity(2, null, false, CancellationToken.None);
        var firstJson = Assert.IsType<JsonResult>(firstResult);
        var firstItems = GetItems(firstJson);
        var firstCursor = GetNextCursor(firstJson);

        Assert.Equal(2, firstItems.Count);
        Assert.All(firstItems, item => Assert.True(item.IsCompleted));
        Assert.NotNull(firstCursor);

        // Second page using cursor.
        var secondResult = await controller.GetActivity(2, firstCursor, false, CancellationToken.None);
        var secondJson = Assert.IsType<JsonResult>(secondResult);
        var secondItems = GetItems(secondJson);
        var secondCursor = GetNextCursor(secondJson);

        Assert.Equal(2, secondItems.Count);
        Assert.All(secondItems, item => Assert.True(item.IsCompleted));
        Assert.NotNull(secondCursor);
        Assert.NotEqual(firstCursor, secondCursor);

        // Ensure no overlap between pages and overall ordering by EndTime.
        Assert.Empty(firstItems.Select(i => i.Timing.EndTime).Intersect(secondItems.Select(i => i.Timing.EndTime)));
        var allEndTimes = firstItems.Concat(secondItems).Select(i => i.Timing.EndTime).ToList();
        Assert.True(allEndTimes.SequenceEqual(allEndTimes.OrderByDescending(t => t)));

        // There should still be at least one more item remaining.
        Assert.False(string.IsNullOrEmpty(secondCursor));
    }

    [Fact]
    public async Task GetActivity_IncludeActive_ReturnsBothActiveAndCompleted()
    {
        var repo = await CreateRepositoryAsync();
        var now = DateTimeOffset.UtcNow;

        // Insert 3 completed entries.
        for (int i = 0; i < 3; i++)
        {
            var endTime = now.AddMinutes(-10 - i);
            var startTime = endTime.AddMinutes(-10);
            var entry = new PlaybackEntry
            {
                PlaybackId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ContentType = ContentType.Movie,
                Title = $"Completed-{i}",
                UserId = Guid.NewGuid(),
                UserName = "User",
                ClientName = "TestClient",
                DeviceName = "TestDevice",
                StartTime = startTime,
                EndTime = endTime,
                IsCompleted = true
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        // Insert 2 active (incomplete) entries.
        for (int i = 0; i < 2; i++)
        {
            var startTime = now.AddMinutes(-5 - i);
            var entry = new PlaybackEntry
            {
                PlaybackId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ContentType = ContentType.Episode,
                Title = $"Active-{i}",
                UserId = Guid.NewGuid(),
                UserName = "User",
                ClientName = "TestClient",
                DeviceName = "TestDevice",
                StartTime = startTime,
                EndTime = null,
                IsCompleted = false
            };

            await repo.AppendAsync(entry, CancellationToken.None);
        }

        var imageCaptureService = CreateMockImageCaptureService();
        var controller = new JellydashController(repo, imageCaptureService);

        // Call GetActivity with includeActive=true.
        var result = await controller.GetActivity(10, null, true, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        var items = GetItems(json);

        Assert.Equal(5, items.Count);

        var activeItems = items.Where(i => !i.IsCompleted).ToList();
        var completedItems = items.Where(i => i.IsCompleted).ToList();

        Assert.Equal(2, activeItems.Count);
        Assert.Equal(3, completedItems.Count);

        // Verify active items have no EndTime.
        Assert.All(activeItems, item => Assert.Null(item.Timing.EndTime));

        // Verify completed items have EndTime.
        Assert.All(completedItems, item => Assert.NotNull(item.Timing.EndTime));

        // Verify overall ordering: items should be sorted by most recent activity.
        // Active items (by StartTime) should appear before older completed items.
        var activeStartTimes = activeItems.Select(i => i.Timing.StartTime).ToList();
        var completedEndTimes = completedItems.Select(i => i.Timing.EndTime!.Value).ToList();

        Assert.True(activeStartTimes.SequenceEqual(activeStartTimes.OrderByDescending(t => t)));
        Assert.True(completedEndTimes.SequenceEqual(completedEndTimes.OrderByDescending(t => t)));
    }

    private static List<PlaybackEntryDto> GetItems(JsonResult json)
    {
        var value = json.Value ?? throw new InvalidOperationException("Result value is null.");
        var response = Assert.IsType<ActivityResponse>(value);
        return response.Items.ToList();
    }

    private static string? GetNextCursor(JsonResult json)
    {
        var value = json.Value ?? throw new InvalidOperationException("Result value is null.");
        var response = Assert.IsType<ActivityResponse>(value);
        return response.NextCursor;
    }
}
