using System.Security.Cryptography;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public class ImageCaptureServiceTests : IDisposable
{
    private readonly string _testImagesPath;
    private readonly Mock<IImageProcessor> _mockImageProcessor;
    private readonly Mock<ILibraryManager> _mockLibraryManager;
    private readonly Mock<ILogger<ImageCaptureService>> _mockLogger;

    public ImageCaptureServiceTests()
    {
        _testImagesPath = Path.Combine(Path.GetTempPath(), "JellydashPluginTests", "ImageCaptureServiceTests");
        Directory.CreateDirectory(Path.Combine(_testImagesPath, "plugins", "Jellydash", "images"));

        _mockImageProcessor = new Mock<IImageProcessor>();
        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockLogger = new Mock<ILogger<ImageCaptureService>>();
    }

    public void Dispose()
    {
        // Clean up test images
        var imagesDir = Path.Combine(_testImagesPath, "plugins", "Jellydash", "images");
        if (Directory.Exists(imagesDir))
        {
            Directory.Delete(imagesDir, true);
        }
    }

    [Fact]
    public async Task CaptureImageAsync_MovieWithImage_ReturnsHashAndSavesFile()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        var movie = new Movie { Id = Guid.NewGuid(), Name = "Test Movie" };
        var imageBytes = GenerateTestImageBytes();
        var expectedHash = ComputeSha256Hash(imageBytes);

        SetupMockImageProcessor(movie, imageBytes);

        // Act
        var hash = await service.CaptureImageAsync(movie, ContentType.Movie, CancellationToken.None);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(expectedHash, hash);

        var imagePath = Path.Combine(_testImagesPath, "plugins", "Jellydash", "images", $"{hash}.jpg");
        Assert.True(File.Exists(imagePath));

        var savedBytes = await File.ReadAllBytesAsync(imagePath);
        Assert.Equal(imageBytes, savedBytes);
    }

    [Fact]
    public async Task CaptureImageAsync_EpisodeWithSeriesImage_UsesParentImage()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        var seriesId = Guid.NewGuid();
        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            Name = "Test Episode",
            ParentId = seriesId
        };

        var series = new Series { Id = seriesId, Name = "Test Series" };
        var imageBytes = GenerateTestImageBytes();
        var expectedHash = ComputeSha256Hash(imageBytes);

        _mockLibraryManager
            .Setup(m => m.GetItemById(seriesId))
            .Returns(series);

        SetupMockImageProcessor(series, imageBytes);

        // Act
        var hash = await service.CaptureImageAsync(episode, ContentType.Episode, CancellationToken.None);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(expectedHash, hash);

        _mockLibraryManager.Verify(m => m.GetItemById(seriesId), Times.Once);
    }

    [Fact]
    public async Task CaptureImageAsync_ItemWithoutImage_ReturnsNull()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        var movie = new Movie { Id = Guid.NewGuid(), Name = "Test Movie" };

        // Movie has no primary image - don't set any image

        // Act
        var hash = await service.CaptureImageAsync(movie, ContentType.Movie, CancellationToken.None);

        // Assert
        Assert.Null(hash);
    }

    [Fact]
    public async Task CaptureImageAsync_DuplicateImage_ReusesExistingHash()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        var movie1 = new Movie { Id = Guid.NewGuid(), Name = "Movie 1" };
        var movie2 = new Movie { Id = Guid.NewGuid(), Name = "Movie 2" };
        var imageBytes = GenerateTestImageBytes();

        SetupMockImageProcessor(movie1, imageBytes);
        SetupMockImageProcessor(movie2, imageBytes);

        // Act
        var hash1 = await service.CaptureImageAsync(movie1, ContentType.Movie, CancellationToken.None);
        var hash2 = await service.CaptureImageAsync(movie2, ContentType.Movie, CancellationToken.None);

        // Assert
        Assert.Equal(hash1, hash2);

        var imagesDir = Path.Combine(_testImagesPath, "plugins", "Jellydash", "images");
        var files = Directory.GetFiles(imagesDir, "*.jpg");
        Assert.Single(files); // Only one file should exist
    }

    [Fact]
    public void GetImagePath_ExistingImage_ReturnsPath()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        var hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"; // Valid SHA256 hash (64 hex chars)
        var expectedPath = Path.Combine(_testImagesPath, "plugins", "Jellydash", "images", $"{hash}.jpg");
        File.WriteAllText(expectedPath, "test");

        // Act
        var path = service.GetImagePath(hash);

        // Assert
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetImagePath_NonExistentImage_ReturnsNull()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        // Act
        var path = service.GetImagePath("nonexistent");

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void DeleteImage_ExistingImage_ReturnsTrue()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        var hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"; // Valid SHA256 hash
        var imagePath = Path.Combine(_testImagesPath, "plugins", "Jellydash", "images", $"{hash}.jpg");
        File.WriteAllText(imagePath, "test");

        // Act
        var deleted = service.DeleteImage(hash);

        // Assert
        Assert.True(deleted);
        Assert.False(File.Exists(imagePath));
    }

    [Fact]
    public void DeleteImage_NonExistentImage_ReturnsFalse()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        // Act
        var deleted = service.DeleteImage("nonexistent");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public void GetAllImagePaths_ReturnsAllJpgFiles()
    {
        // Arrange
        var service = new ImageCaptureService(
            _mockImageProcessor.Object,
            _mockLibraryManager.Object,
            _mockLogger.Object,
            _testImagesPath);

        var imagesDir = Path.Combine(_testImagesPath, "plugins", "Jellydash", "images");
        File.WriteAllText(Path.Combine(imagesDir, "image1.jpg"), "test1");
        File.WriteAllText(Path.Combine(imagesDir, "image2.jpg"), "test2");
        File.WriteAllText(Path.Combine(imagesDir, "other.txt"), "text");

        // Act
        var paths = service.GetAllImagePaths();

        // Assert
        Assert.Equal(2, paths.Length);
        Assert.All(paths, p => Assert.EndsWith(".jpg", p));
    }

    private static byte[] GenerateTestImageBytes()
    {
        // Generate deterministic test image bytes
        var bytes = new byte[1024];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i % 256);
        }
        return bytes;
    }

    private static string ComputeSha256Hash(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void SetupMockImageProcessor(BaseItem item, byte[] imageBytes)
    {
        // Set image path directly on the item's internal image info
        var imagePath = "/path/to/image.jpg";
        item.SetImage(new ItemImageInfo
        {
            Path = imagePath,
            Type = ImageType.Primary
        }, 0);

        var tempImagePath = Path.Combine(Path.GetTempPath(), $"test-image-{Guid.NewGuid()}.jpg");
        File.WriteAllBytes(tempImagePath, imageBytes);

        _mockImageProcessor
            .Setup(p => p.ProcessImage(It.Is<ImageProcessingOptions>(opt =>
                opt.Item.Id == item.Id &&
                opt.Image.Type == ImageType.Primary)))
            .ReturnsAsync((tempImagePath, "image/jpeg", DateTime.UtcNow));
    }
}
