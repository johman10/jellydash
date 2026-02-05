using System;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellydash.Events;

/// <summary>
/// Records Jellydash playback entries when playback starts, progresses, and stops.
/// </summary>
public class PlaybackTracker :
    IEventConsumer<PlaybackStartEventArgs>,
    IEventConsumer<PlaybackProgressEventArgs>,
    IEventConsumer<PlaybackStopEventArgs>,
    IEventConsumer<SessionEndedEventArgs>
{
    private readonly ILogger<PlaybackTracker> _logger;
    private readonly PlaybackEntryRepository _repository;
    private readonly ImageCaptureService _imageCaptureService;
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTracker"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="databaseHelper">DatabaseHelper instance.</param>
    /// <param name="imageCaptureService">Image capture service.</param>
    /// <param name="configurationManager">Server configuration manager for accessing resume percentage thresholds.</param>
    public PlaybackTracker(
        ILogger<PlaybackTracker> logger,
        DatabaseHelper databaseHelper,
        ImageCaptureService imageCaptureService,
        IServerConfigurationManager configurationManager)
    {
        _logger = logger;
        _repository = new PlaybackEntryRepository(databaseHelper);
        _imageCaptureService = imageCaptureService;
        _configurationManager = configurationManager;
    }

    /// <inheritdoc />
    public async Task OnEvent(PlaybackStartEventArgs eventArgs)
    {
        try
        {
            var media = eventArgs.MediaInfo;
            if (media is null || !IsSupportedItemType(media) || eventArgs.Users.Count == 0)
            {
                _logger.LogDebug("Irrelavant playback event found, media: {Media}, type: {Type}, users: {UserCount}", media, media?.Type, eventArgs.Users.Count);
                return;
            }

            )
            var contentType = ContentTypeExtensions.FromBaseItemKind(media.Type);
            var imageHash = await _imageCaptureService.CaptureImageAsync(eventArgs.Item, contentType, default).ConfigureAwait(false);
            var entry = PlaybackEntry.FromStartEvent(eventArgs, imageHash);

            _logger.LogInformation("Playback started event with playback ID {PlaybackId} (based on {SessionId}, {PlaylistItemId}, {MediaId})", entry.PlaybackId, eventArgs.Session.Id, eventArgs.Session.PlaylistItemId, eventArgs.MediaInfo.Id);
            await _repository.AppendAsync(entry, default).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback start event for Jellydash activity.");
        }
    }

    /// <inheritdoc />
    public async Task OnEvent(PlaybackProgressEventArgs eventArgs)
    {
        try
        {
            var media = eventArgs.MediaInfo;
            if (media is null || !IsSupportedItemType(media) || eventArgs.Users.Count == 0)
            {
                return;
            }

            var playbackId = PlaybackEntry.GeneratePlaybackId(eventArgs.Session.Id, eventArgs.Session.PlaylistItemId, eventArgs.MediaInfo.Id);
            var existing = await _repository.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, default).ConfigureAwait(false);
            string? imageHash = null;
            if (existing?.ItemImageHash is null)
            {
                var contentType = ContentTypeExtensions.FromBaseItemKind(media.Type);
                imageHash = await _imageCaptureService.CaptureImageAsync(eventArgs.Item, contentType, default).ConfigureAwait(false);
            }

            var entry = PlaybackEntry.FromProgressEvent(existing, eventArgs, imageHash);
            _logger.LogInformation("Playback progress event for playback ID {PlaybackId} (based on {SessionId}, {PlaylistItemId}, {MediaId})", entry.PlaybackId, eventArgs.Session.Id, eventArgs.Session.PlaylistItemId, eventArgs.MediaInfo.Id);
            await _repository.Upsert(entry, default).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback progress event for Jellydash activity.");
        }
    }

    /// <inheritdoc />
    public async Task OnEvent(PlaybackStopEventArgs eventArgs)
    {
        try
        {
            var media = eventArgs.MediaInfo;
            if (media is null || !IsSupportedItemType(media) || eventArgs.Users.Count == 0)
            {
                return;
            }

            var playbackId = PlaybackEntry.GeneratePlaybackId(eventArgs.Session.Id, eventArgs.Session.PlaylistItemId, eventArgs.MediaInfo.Id);
            var existing = await _repository.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, default).ConfigureAwait(false);
            string? imageHash = null;
            if (existing?.ItemImageHash is null)
            {
                var contentType = ContentTypeExtensions.FromBaseItemKind(media.Type);
                imageHash = await _imageCaptureService.CaptureImageAsync(eventArgs.Item, contentType, default).ConfigureAwait(false);
            }

            var minResumePct = _configurationManager.Configuration.MinResumePct;
            var maxResumePct = _configurationManager.Configuration.MaxResumePct;
            var entry = PlaybackEntry.FromStopEvent(existing, eventArgs, imageHash, maxResumePct);
            _logger.LogInformation("Playback stop event for playback ID {PlaybackId} (based on {SessionId}, {PlaylistItemId}, {MediaId})", entry.PlaybackId, eventArgs.Session.Id, eventArgs.Session.PlaylistItemId, eventArgs.MediaInfo.Id);
            if (entry.ShouldTrackInHistory(minResumePct))
            {
                await _repository.Upsert(entry, default).ConfigureAwait(false);
            }
            else if (existing is not null)
            {
                await _repository.DeleteByIdAsync(existing.Id, default).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback stop event for Jellydash activity.");
        }
    }

    /// <inheritdoc />
    public async Task OnEvent(SessionEndedEventArgs eventArgs)
    {
        try
        {
            var session = eventArgs.Argument;
            var media = session.NowPlayingItem;
            if (media is null || !IsSupportedItemType(media) || session.UserId == Guid.Empty)
            {
                return;
            }

            var playbackId = PlaybackEntry.GeneratePlaybackId(session.Id, session.PlaylistItemId, media.Id);
            var existing = await _repository.GetRecentlyIncompletedByPlaybackIdAsync(playbackId, default).ConfigureAwait(false);
            if (existing is null)
            {
                return;
            }

            var minResumePct = _configurationManager.Configuration.MinResumePct;
            var maxResumePct = _configurationManager.Configuration.MaxResumePct;
            var entry = PlaybackEntry.FromSessionEndedEvent(existing, eventArgs, maxResumePct);
            _logger.LogInformation("Playback stop event for playback ID {PlaybackId} (based on {SessionId}, {PlaylistItemId}, {MediaId})", entry.PlaybackId, session.Id, session.PlaylistItemId, session.NowPlayingItem.Id);
            if (entry.ShouldTrackInHistory(minResumePct))
            {
                await _repository.Upsert(entry, default).ConfigureAwait(false);
            }
            else
            {
                await _repository.DeleteByIdAsync(existing.Id, default).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling session ended event for Jellydash activity.");
        }
    }

    private static bool IsSupportedItemType(BaseItemDto media)
    {
        var type = media.Type;
        return type == BaseItemKind.Movie || type == BaseItemKind.Episode;
    }
}
