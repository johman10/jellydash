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
/// Records Jellydash playback entries when playback starts and stops.
/// </summary>
public class PlaybackTracker : IEventConsumer<PlaybackStartEventArgs>, IEventConsumer<PlaybackStopEventArgs>
{
    private readonly ILogger<PlaybackTracker> _logger;
    private readonly PlaybackEntryRepository _repository;

    // Track the start of each play session so we can compute contiguous spans.
    private static readonly ConcurrentDictionary<string, PlaybackEntry> Seeds = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTracker"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="databaseHelper">DatabaseHelper instance.</param>
    public PlaybackTracker(ILogger<PlaybackTracker> logger, DatabaseHelper databaseHelper)
    {
        _logger = logger;
        _repository = new PlaybackEntryRepository(databaseHelper);
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

            var seed = CreateSeedEntry(
                media,
                user.Id,
                user.Username,
                eventArgs.ClientName,
                eventArgs.DeviceName,
                eventArgs.Session?.DeviceId,
                runtimeTicks,
                startTicks,
                eventArgs.Session?.TranscodingInfo);

            Seeds[key] = seed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback start event for Jellydash activity.");
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
                var fallbackUser = eventArgs.Users[0];
                seed = CreateSeedEntry(
                    media,
                    fallbackUser.Id,
                    fallbackUser.Username,
                    eventArgs.ClientName,
                    eventArgs.DeviceName,
                    eventArgs.Session?.DeviceId,
                    media.RunTimeTicks,
                    eventArgs.PlaybackPositionTicks ?? 0,
                    eventArgs.Session?.TranscodingInfo);
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

            var isCompleted = runtimeTicks.HasValue && runtimeTicks.Value > 0 && endPercent >= 95.0;

            var entry = new PlaybackEntry
            {
                ItemId = seed.ItemId,
                ContentKind = seed.ContentKind,
                DisplayTitle = seed.DisplayTitle,
                PrimaryImageUrl = seed.PrimaryImageUrl,
                PrimaryGenre = seed.PrimaryGenre,
                Year = seed.Year,
                SeriesName = seed.SeriesName,
                SeasonNumber = seed.SeasonNumber,
                EpisodeNumber = seed.EpisodeNumber,
                UserId = seed.UserId,
                UserName = seed.UserName,
                UserImageUrl = seed.UserImageUrl,
                ClientName = seed.ClientName,
                DeviceName = seed.DeviceName,
                DeviceId = seed.DeviceId,
                StartUtc = seed.StartUtc,
                EndUtc = DateTime.UtcNow,
                RuntimeTicks = runtimeTicks,
                StartPositionTicks = seed.StartPositionTicks,
                EndPositionTicks = endTicks,
                StartPercentage = startPercent,
                EndPercentage = endPercent,
                IsCompleted = isCompleted,
                IsPaused = false,
                IsVideoDirect = seed.IsVideoDirect,
                IsAudioDirect = seed.IsAudioDirect,
                TranscodeBitrate = seed.TranscodeBitrate,
                HardwareAcceleration = seed.HardwareAcceleration,
                TranscodedVideoCodec = seed.TranscodedVideoCodec,
                TranscodedAudioCodec = seed.TranscodedAudioCodec,
                TranscodeReasonsJson = seed.TranscodeReasonsJson
            };

            await _repository.AppendAsync(entry, default).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback stop event for Jellydash activity.");
        }
    }

    private static PlaybackEntry CreateSeedEntry(
        BaseItemDto media,
        Guid userId,
        string userName,
        string clientName,
        string deviceName,
        string? deviceId,
        long? runtimeTicks,
        long startTicks,
        MediaBrowser.Model.Session.TranscodingInfo? transcodeInfo)
    {
        var contentKind = media.Type switch
        {
            BaseItemKind.Movie => ContentKind.Movie,
            BaseItemKind.Episode => ContentKind.Episode,
            _ => ContentKind.Other
        };

        string displayTitle;
        string? seriesName = null;
        int? seasonNumber = null;
        int? episodeNumber = null;
        int? year = null;

        if (media.Type == BaseItemKind.Movie)
        {
            displayTitle = media.Name ?? string.Empty;
            year = media.ProductionYear;
        }
        else if (media.Type == BaseItemKind.Episode)
        {
            displayTitle = media.SeriesName ?? media.Name ?? string.Empty;
            seriesName = media.SeriesName;
            seasonNumber = media.ParentIndexNumber;
            episodeNumber = media.IndexNumber;
        }
        else
        {
            displayTitle = media.Name ?? string.Empty;
            year = media.ProductionYear;
        }

        var seed = new PlaybackEntry
        {
            ItemId = media.Id,
            ContentKind = contentKind,
            DisplayTitle = displayTitle,
            PrimaryImageUrl = null,
            PrimaryGenre = media.Genres?.FirstOrDefault(),
            Year = year,
            SeriesName = seriesName,
            SeasonNumber = seasonNumber,
            EpisodeNumber = episodeNumber,
            UserId = userId,
            UserName = userName,
            UserImageUrl = null,
            ClientName = clientName,
            DeviceName = deviceName,
            DeviceId = deviceId,
            StartUtc = DateTime.UtcNow,
            RuntimeTicks = runtimeTicks,
            StartPositionTicks = startTicks,
            IsCompleted = false,
            IsPaused = false,
            IsVideoDirect = transcodeInfo?.IsVideoDirect ?? true,
            IsAudioDirect = transcodeInfo?.IsAudioDirect ?? true,
            TranscodeBitrate = null,
            HardwareAcceleration = transcodeInfo?.HardwareAccelerationType?.ToString(),
            TranscodedVideoCodec = transcodeInfo?.VideoCodec,
            TranscodedAudioCodec = transcodeInfo?.AudioCodec,
            TranscodeReasonsJson = null
        };

        return seed;
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
}
