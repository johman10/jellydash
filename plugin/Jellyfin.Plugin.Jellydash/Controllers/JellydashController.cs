using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public class JellydashController : ControllerBase
    {
        private readonly HistoryRepository _historyRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="JellydashController"/> class.
        /// </summary>
        /// <param name="historyRepository">The history repository.</param>
        public JellydashController(HistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        /// <summary>
        /// Returns a page of recent history entries using cursor-based pagination.
        /// </summary>
        /// <param name="limit">Maximum number of entries to return (max 100, default 20).</param>
        /// <param name="cursor">An opaque cursor returned from a previous page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A page of recent history entries and a cursor for the next page, if any.</returns>
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetHistory([FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken)
        {
            var pageSize = limit.HasValue && limit.Value > 0 ? Math.Min(limit.Value, 100) : 20;

            long? beforeId = null;
            DateTime? beforeEndUtc = null;

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

            var (entries, lastId, lastEndUtc) = await _historyRepository
                .GetPageAsync(pageSize, beforeId, beforeEndUtc, cancellationToken)
                .ConfigureAwait(false);

            string? nextCursor = null;
            if (lastId.HasValue && lastEndUtc.HasValue)
            {
                nextCursor = EncodeCursor(lastEndUtc.Value, lastId.Value);
            }

            return Ok(new
            {
                items = entries,
                nextCursor
            });
        }

        private static string EncodeCursor(DateTime endUtc, long id)
        {
            var payload = endUtc.ToString("O", CultureInfo.InvariantCulture)
                          + "|" + id.ToString(CultureInfo.InvariantCulture);
            var bytes = Encoding.UTF8.GetBytes(payload);
            return Convert.ToBase64String(bytes);
        }

        private static (DateTime EndUtc, long Id) DecodeCursor(string cursor)
        {
            var bytes = Convert.FromBase64String(cursor);
            var payload = Encoding.UTF8.GetString(bytes);
            var parts = payload.Split('|');
            if (parts.Length != 2)
            {
                throw new FormatException("Invalid cursor payload.");
            }

            var endUtc = DateTime.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            var id = long.Parse(parts[1], CultureInfo.InvariantCulture);
            return (endUtc, id);
        }
    }
}
