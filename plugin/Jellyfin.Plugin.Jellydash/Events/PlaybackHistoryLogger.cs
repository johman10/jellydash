using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellydash.Events;

/// <summary>
/// Records Jellydash history entries when playback starts and stops.
/// </summary>
public class PlaybackHistoryLogger : IEventConsumer<PlaybackStartEventArgs>, IEventConsumer<PlaybackStopEventArgs>
{
    private readonly ILogger<PlaybackHistoryLogger> _logger;
    private readonly HistoryRepository _repository = new();

    // Track the start of each play session so we can compute contiguous spans.
    private static readonly ConcurrentDictionary<string, HistorySeed> Seeds = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackHistoryLogger"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public PlaybackHistoryLogger(ILogger<PlaybackHistoryLogger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task OnEvent(PlaybackStartEventArgs eventArgs)
    {
        try
        {
            var media = eventArgs.MediaInfo;
            if (media is null)
            {
                return Task.CompletedTask;
            }

            // Only track movies and episodes for now.
            if (!IsSupportedItemType(media))
            {
                return Task.CompletedTask;
            }

            if (eventArgs.Users.Count == 0)
            {
                return Task.CompletedTask;
            }

            var user = eventArgs.Users[0];
            var key = GetSeedKey(eventArgs.PlaySessionId, eventArgs.Session?.Id, media.Id);
            if (key is null)
            {
                return Task.CompletedTask;
            }

            var runtimeTicks = media.RunTimeTicks;
            var startTicks = eventArgs.PlaybackPositionTicks ?? 0;

            var transcodeInfo = eventArgs.Session?.TranscodingInfo;

            var seed = new HistorySeed
            {
                UserId = user.Id,
                UserName = user.Username,
                ItemId = media.Id,
                MediaType = media.MediaType.ToString(),
                ItemName = media.Name ?? string.Empty,
                SeriesName = media.SeriesName,
                SeasonNumber = media.ParentIndexNumber,
                EpisodeNumber = media.IndexNumber,
                ProductionYear = media.ProductionYear,
                PrimaryGenre = media.Genres?.FirstOrDefault(),
                RuntimeTicks = runtimeTicks,
                StartPositionTicks = startTicks,
                StartUtc = DateTime.UtcNow,
                ClientName = eventArgs.ClientName,
                DeviceName = eventArgs.DeviceName,
                Bitrate = transcodeInfo?.Bitrate,
                IsVideoDirectStream = transcodeInfo?.IsVideoDirect ?? false,
                IsAudioDirectStream = transcodeInfo?.IsAudioDirect ?? false,
                TranscodeVideoCodec = transcodeInfo?.VideoCodec ?? string.Empty,
                TranscodeAudioCodec = transcodeInfo?.AudioCodec ?? string.Empty,
                TranscodeContainer = transcodeInfo?.Container ?? string.Empty,
                TranscodeHardwareAcceleration = transcodeInfo?.HardwareAccelerationType?.ToString() ?? string.Empty
            };

            Seeds[key] = seed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback start event for Jellydash history.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnEvent(PlaybackStopEventArgs eventArgs)
    {
        try
        {
            var media = eventArgs.MediaInfo;
            if (media is null)
            {
                return;
            }

            if (!IsSupportedItemType(media))
            {
                return;
            }

            if (eventArgs.Users.Count == 0)
            {
                return;
            }

            var key = GetSeedKey(eventArgs.PlaySessionId, eventArgs.Session?.Id, media.Id);
            if (key is null)
            {
                return;
            }

            if (!Seeds.TryRemove(key, out var seed))
            {
                // If we never saw a start, treat the span as beginning at the current position.
                seed = new HistorySeed
                {
                    UserId = eventArgs.Users[0].Id,
                    UserName = eventArgs.Users[0].Username,
                    ItemId = media.Id,
                    MediaType = media.MediaType.ToString(),
                    ItemName = media.Name ?? string.Empty,
                    SeriesName = media.SeriesName,
                    SeasonNumber = media.ParentIndexNumber,
                    EpisodeNumber = media.IndexNumber,
                    ProductionYear = media.ProductionYear,
                    PrimaryGenre = media.Genres?.FirstOrDefault(),
                    RuntimeTicks = media.RunTimeTicks,
                    StartPositionTicks = eventArgs.PlaybackPositionTicks ?? 0,
                    StartUtc = DateTime.UtcNow,
                    ClientName = eventArgs.ClientName,
                    DeviceName = eventArgs.DeviceName
                };
            }

            var endTicks = eventArgs.PlaybackPositionTicks ?? 0;
            var runtimeTicks = seed.RuntimeTicks ?? media.RunTimeTicks;

            double startPercent = 0;
            double endPercent = 0;
            if (runtimeTicks.HasValue && runtimeTicks.Value > 0)
            {
                startPercent = (double)(seed.StartPositionTicks ?? 0) / runtimeTicks.Value * 100.0;
                endPercent = (double)endTicks / runtimeTicks.Value * 100.0;
            }

            var entry = new HistoryEntry
            {
                UserId = seed.UserId,
                UserName = seed.UserName,
                UserImageUrl = null,
                ItemId = seed.ItemId,
                ClientName = seed.ClientName,
                DeviceName = seed.DeviceName,
                MediaType = seed.MediaType,
                ItemName = seed.ItemName,
                SeriesName = seed.SeriesName,
                SeasonNumber = seed.SeasonNumber,
                EpisodeNumber = seed.EpisodeNumber,
                ProductionYear = seed.ProductionYear,
                PrimaryGenre = seed.PrimaryGenre,
                PrimaryImagePath = null,
                StartUtc = seed.StartUtc,
                EndUtc = DateTime.UtcNow,
                RuntimeTicks = runtimeTicks,
                StartPositionTicks = seed.StartPositionTicks,
                EndPositionTicks = endTicks,
                StartPercentage = startPercent,
                EndPercentage = endPercent,
                IsDownload = false,
                DownloadSizeBytes = null,
                DownloadedBytes = null,
                DownloadProgressPercent = null,
                Bitrate = seed.Bitrate,
                IsVideoDirectStream = seed.IsVideoDirectStream,
                IsAudioDirectStream = seed.IsAudioDirectStream,
                IsTranscoding = !seed.IsVideoDirectStream || !seed.IsAudioDirectStream,
                TranscodeVideoCodec = seed.TranscodeVideoCodec,
                TranscodeAudioCodec = seed.TranscodeAudioCodec,
                TranscodeContainer = seed.TranscodeContainer,
                TranscodeHardwareAcceleration = seed.TranscodeHardwareAcceleration
            };

            await _repository.AppendAsync(entry, default).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback stop event for Jellydash history.");
        }
    }

    private static bool IsSupportedItemType(BaseItemDto media)
    {
        var type = media.Type;
        return type == BaseItemKind.Movie || type == BaseItemKind.Episode;
    }

    private static string? GetSeedKey(string? playSessionId, string? sessionId, Guid itemId)
    {
        if (!string.IsNullOrEmpty(playSessionId))
        {
            return playSessionId;
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            return sessionId + ":" + itemId.ToString("N");
        }

        return null;
    }

    private sealed class HistorySeed
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public Guid ItemId { get; set; }

        public string MediaType { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string? SeriesName { get; set; }

        public int? SeasonNumber { get; set; }

        public int? EpisodeNumber { get; set; }

        public int? ProductionYear { get; set; }

        public string? PrimaryGenre { get; set; }

        public long? RuntimeTicks { get; set; }

        public long? StartPositionTicks { get; set; }

        public DateTime StartUtc { get; set; }

        public string? ClientName { get; set; }

        public string? DeviceName { get; set; }

        public long? Bitrate { get; set; }

        public bool IsVideoDirectStream { get; set; }

        public bool IsAudioDirectStream { get; set; }

        public string TranscodeVideoCodec { get; set; } = string.Empty;

        public string TranscodeAudioCodec { get; set; } = string.Empty;

        public string TranscodeContainer { get; set; } = string.Empty;

        public string TranscodeHardwareAcceleration { get; set; } = string.Empty;
    }
}
