using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellydash.Services;

/// <summary>
/// Service for capturing, resizing, and caching item images.
/// </summary>
public class ImageCaptureService
{
    private const int MaxImageDimension = 720;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<ImageCaptureService> _logger;
    private readonly string _imagesPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageCaptureService"/> class.
    /// </summary>
    /// <param name="imageProcessor">Jellyfin's image processor for retrieving and resizing images.</param>
    /// <param name="libraryManager">Library manager for item lookups.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="dataPath">Plugin data path where images will be stored.</param>
    public ImageCaptureService(
        IImageProcessor imageProcessor,
        ILibraryManager libraryManager,
        ILogger<ImageCaptureService> logger,
        string dataPath)
    {
        _imageProcessor = imageProcessor;
        _libraryManager = libraryManager;
        _logger = logger;
        _imagesPath = Path.Combine(dataPath, "plugins", "Jellydash", "images");
        Directory.CreateDirectory(_imagesPath);
    }

    /// <summary>
    /// Captures and caches an image for the given item.
    /// </summary>
    /// <param name="item">The media item to capture an image for.</param>
    /// <param name="contentType">The type of content (movie, episode, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SHA256 hash of the cached image, or null if the image could not be captured.</returns>
    public async Task<string?> CaptureImageAsync(
        BaseItem item,
        Models.ContentType contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            // For episodes, use the series poster instead
            var imageItem = item;
            if (contentType == Models.ContentType.Episode)
            {
                var parent = _libraryManager.GetItemById(item.ParentId);
                if (parent != null)
                {
                    imageItem = parent;
                }
            }

            // Check if the item has a primary image
            if (!imageItem.HasImage(ImageType.Primary))
            {
                _logger.LogDebug("Item {ItemId} has no primary image", item.Id);
                return null;
            }

            var imageInfo = imageItem.GetImageInfo(ImageType.Primary, 0);
            if (imageInfo == null)
            {
                _logger.LogDebug("Item {ItemId} primary image info is null", item.Id);
                return null;
            }

            _logger.LogDebug("Primary image info for item {ItemId}: {@ImageInfo}", item.Id, imageInfo);

            // Process the image with resizing
            // Always force JPG output for consistency and smaller file sizes
            var imageOptions = new ImageProcessingOptions
            {
                Image = imageInfo,
                ImageIndex = 0,
                Item = imageItem,
                MaxHeight = MaxImageDimension,
                MaxWidth = MaxImageDimension,
                Quality = 90,
                BackgroundColor = string.Empty,
                ForegroundLayer = null,
                SupportedOutputFormats = new[] { ImageFormat.Jpg }
            };

            var processedImage = await _imageProcessor.ProcessImage(imageOptions).ConfigureAwait(false);

            // Read the processed image from the file path
            var imageBytes = await File.ReadAllBytesAsync(processedImage.Path, cancellationToken).ConfigureAwait(false);

            // Compute SHA256 hash
            var hash = SHA256.HashData(imageBytes);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();

            // Check if image already exists (deduplication)
            var imagePath = Path.Combine(_imagesPath, $"{hashString}.jpg");
            if (File.Exists(imagePath))
            {
                _logger.LogDebug("Image {Hash} already exists, skipping write", hashString);
                return hashString;
            }

            // Save the image
            await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Captured and saved image {Hash} for item {ItemId}", hashString, item.Id);

            return hashString;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture image for item {ItemId}", item.Id);
            return null;
        }
    }

    /// <summary>
    /// Gets the file path for a cached image by its hash.
    /// </summary>
    /// <param name="hash">The SHA256 hash of the image.</param>
    /// <returns>The full file path to the cached image, or null if it doesn't exist.</returns>
#pragma warning disable CA3003 // Hash is validated to be 64 hex characters only
    public string? GetImagePath(string hash)
    {
        var imagePath = Path.Combine(_imagesPath, $"{hash}.jpg");
        return File.Exists(imagePath) ? imagePath : null;
    }
#pragma warning restore CA3003

    /// <summary>
    /// Deletes a cached image by its hash.
    /// </summary>
    /// <param name="hash">The SHA256 hash of the image to delete.</param>
    /// <returns>True if the image was deleted, false if it didn't exist.</returns>
    public bool DeleteImage(string hash)
    {
        var imagePath = Path.Combine(_imagesPath, $"{hash}.jpg");
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
            _logger.LogDebug("Deleted orphaned image {Hash}", hash);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all cached image file paths.
    /// </summary>
    /// <returns>An array of file paths for all cached images.</returns>
    public string[] GetAllImagePaths()
    {
        return Directory.GetFiles(_imagesPath, "*.jpg", SearchOption.TopDirectoryOnly);
    }
}
