using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellydash.Controllers
{
    /// <summary>
    /// Controller exposing Jellydash plugin HTTP endpoints.
    /// </summary>
    [Route("Jellydash")]
    [ApiController]
    [Produces("application/json")]
    public class JellydashController : ControllerBase
    {
        private readonly PlaybackEntryRepository _activityRepository;
        private readonly ImageCaptureService _imageCaptureService;

        private static readonly JsonSerializerOptions JsonResultOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JellydashController"/> class.
        /// </summary>
        /// <param name="activityRepository">The activity repository.</param>
        /// <param name="imageCaptureService">The image capture service.</param>
        public JellydashController(
            PlaybackEntryRepository activityRepository,
            ImageCaptureService imageCaptureService)
        {
            _activityRepository = activityRepository;
            _imageCaptureService = imageCaptureService;
        }

        /// <summary>
        /// Returns a page of recent playback entries using cursor-based pagination.
        /// </summary>
        /// <param name="limit">Maximum number of entries to return (max 100, default 20).</param>
        /// <param name="cursor">An opaque cursor returned from a previous page.</param>
        /// <param name="includeActive">Whether to include active (not completed) entries.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A page of recent activity entries and a cursor for the next page, if any.</returns>
        [HttpGet("activity")]
        [Authorize]
        public async Task<IActionResult> GetActivity([FromQuery] int? limit, [FromQuery] string? cursor, [FromQuery] bool includeActive, CancellationToken cancellationToken)
        {
            var pageSize = limit.HasValue && limit.Value > 0 ? Math.Min(limit.Value, 100) : 20;

            int? beforeId = null;
            DateTimeOffset? beforeEndUtc = null;

            if (!string.IsNullOrWhiteSpace(cursor))
            {
                try
                {
                    (beforeEndUtc, beforeId) = DecodeCursor(cursor);
                }
                catch (FormatException)
                {
                    return BadRequest("Invalid cursor.");
                }
            }

            var (entries, lastId, lastEndUtc) = await _activityRepository
                .GetActivitiesAsync(pageSize, beforeId, beforeEndUtc, includeActive, cancellationToken)
                .ConfigureAwait(false);

            string? nextCursor = null;
            if (lastId.HasValue && lastEndUtc.HasValue)
            {
                nextCursor = EncodeCursor(lastEndUtc.Value, lastId.Value);
            }

            var dtoList = new List<PlaybackEntryDto>(entries.Count);
            foreach (var entry in entries)
            {
                dtoList.Add(PlaybackEntryDto.FromPlaybackEntry(entry));
            }

            var dtoItems = new Collection<PlaybackEntryDto>(dtoList);
            return new JsonResult(new ActivityResponse(items: dtoItems, nextCursor: nextCursor), JsonResultOptions);
        }

        private static string EncodeCursor(DateTimeOffset endUtc, long id)
        {
            var payload = endUtc.ToString("O", CultureInfo.InvariantCulture)
                          + "|" + id.ToString(CultureInfo.InvariantCulture);
            var bytes = Encoding.UTF8.GetBytes(payload);
            return Convert.ToBase64String(bytes);
        }

        private static (DateTimeOffset EndUtc, int Id) DecodeCursor(string cursor)
        {
            var bytes = Convert.FromBase64String(cursor);
            var payload = Encoding.UTF8.GetString(bytes);
            var parts = payload.Split('|');
            if (parts.Length != 2)
            {
                throw new FormatException("Invalid cursor payload.");
            }

            var endUtc = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            var id = int.Parse(parts[1], CultureInfo.InvariantCulture);
            return (endUtc, id);
        }

        /// <summary>
        /// Serves a cached image by its hash.
        /// </summary>
        /// <param name="hash">The SHA256 hash of the image.</param>
        /// <returns>The image file or 404 if not found.</returns>
        [HttpGet("images/{hash}")]
        [Authorize]
        public IActionResult GetImage([FromRoute] string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return BadRequest("Hash is required.");
            }

            var imagePath = _imageCaptureService.GetImagePath(hash);
            if (imagePath == null)
            {
                return NotFound();
            }

            // Serve the image with caching headers since hash-addressed content is immutable
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return PhysicalFile(imagePath, "image/jpeg");
        }
    }
}
