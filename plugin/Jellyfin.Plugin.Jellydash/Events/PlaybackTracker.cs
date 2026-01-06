using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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

            var entry = PlaybackEntry.FromStartEvent(eventArgs);
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
            var entry = PlaybackEntry.FromProgressEvent(existing, eventArgs);
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
            var entry = PlaybackEntry.FromStopEvent(existing, eventArgs);
            await _repository.Upsert(entry, default).ConfigureAwait(false);
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

            var entry = PlaybackEntry.FromSessionEndedEvent(existing, eventArgs);
            await _repository.Upsert(existing, default).ConfigureAwait(false);
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
