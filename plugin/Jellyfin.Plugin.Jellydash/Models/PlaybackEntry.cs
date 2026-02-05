using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents a single contiguous playback span for a media item as stored in SQLite.
/// </summary>
public class PlaybackEntry
{
    // Identity

    /// <summary>
    /// Gets or sets the record ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin playback identifier associated with this playback span.
    /// </summary>
    public required Guid PlaybackId { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin session identifier used to generate the PlaybackId.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin playlist item identifier used to generate the PlaybackId.
    /// </summary>
    public string? PlaylistItemId { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item identifier.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin parent item identifier.
    /// Only relevant for episodes within a series.
    /// </summary>
    public Guid? ParentItemId { get; set; }

    /// <summary>
    /// Gets or sets the kind of content (movie, episode, or other).
    /// </summary>
    public ContentType ContentType { get; set; }

    /// <summary>
    /// Gets or sets the primary display title (series or movie name).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets the genre of the item, if known.
    /// </summary>
    public Collection<string> Genres { get; init; } = new Collection<string>();

    /// <summary>
    /// Gets or sets the production year when the content is a movie.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the series name when the content is an episode.
    /// </summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season number when the content is an episode.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number when the content is an episode.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    // User

    /// <summary>
    /// Gets or sets the Jellyfin user identifier associated with this entry.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name for display.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary image tag for the user, used to retrieve the user's profile image.
    /// </summary>
    public string? UserPrimaryImageTag { get; set; }

    // Images

    /// <summary>
    /// Gets or sets the SHA256 hash of the cached item image, if captured.
    /// </summary>
    public string? ItemImageHash { get; set; }

    // Client

    /// <summary>
    /// Gets or sets the client application name.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional device identifier.
    /// </summary>
    public string? DeviceId { get; set; }

    // Timing / progress

    /// <summary>
    /// Gets or sets the UTC timestamp when this span started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this span ended, if completed.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entry was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the total runtime of the item in ticks.
    /// </summary>
    public long? RuntimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the starting playback position in ticks.
    /// </summary>
    public long? StartPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the ending playback position in ticks, if completed.
    /// </summary>
    public long? EndPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this playback span has fully completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether playback was paused at the end of this span.
    /// </summary>
    public bool IsPaused { get; set; }

    // Stream: video

    /// <summary>
    /// Gets or sets the video codec (for example, h264, hevc).
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the container format used for the video.
    /// </summary>
    public string? VideoContainer { get; set; }

    /// <summary>
    /// Gets or sets the video range (for example, SDR, HDR10, DolbyVision).
    /// </summary>
    public string? VideoRange { get; set; }

    /// <summary>
    /// Gets or sets the approximate video bitrate in bits per second.
    /// </summary>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the bit depth of the video.
    /// </summary>
    public int? VideoBitDepth { get; set; }

    /// <summary>
    /// Gets or sets the encoded video height in pixels.
    /// </summary>
    public int? VideoHeight { get; set; }

    /// <summary>
    /// Gets or sets the encoded video width in pixels.
    /// </summary>
    public int? VideoWidth { get; set; }

    // Stream: audio

    /// <summary>
    /// Gets or sets the language of the audio track.
    /// </summary>
    public string? AudioLanguage { get; set; }

    /// <summary>
    /// Gets or sets the audio codec (for example, ac3, eac3, dts).
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the channel layout (for example, 2.0, 5.1).
    /// </summary>
    public string? AudioLayout { get; set; }

    /// <summary>
    /// Gets or sets the approximate audio bitrate in bits per second.
    /// </summary>
    public int? AudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets the audio sample rate in Hz.
    /// </summary>
    public int? AudioSampleRate { get; set; }

    // Stream: subtitle

    /// <summary>
    /// Gets or sets a value indicating whether the subtitle track is forced.
    /// </summary>
    public bool? SubtitleIsForced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the subtitle track is for the hearing impaired.
    /// </summary>
    public bool? SubtitleIsHearingImpaired { get; set; }

    /// <summary>
    /// Gets or sets the subtitle codec.
    /// </summary>
    public string? SubtitleCodec { get; set; }

    /// <summary>
    /// Gets or sets the language of the subtitle track.
    /// </summary>
    public string? SubtitleLanguage { get; set; }

    // Transcoding

    /// <summary>
    /// Gets or sets a value indicating whether the video stream was direct during this span.
    /// </summary>
    public bool IsVideoDirect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio stream was direct during this span.
    /// </summary>
    public bool IsAudioDirect { get; set; }

    /// <summary>
    /// Gets or sets the overall transcoded bitrate in bits per second, if known.
    /// </summary>
    public int? TranscodeBitrate { get; set; }

    /// <summary>
    /// Gets or sets the hardware acceleration mode used, if any.
    /// </summary>
    public string? HardwareAcceleration { get; set; }

    /// <summary>
    /// Gets or sets the codec of the transcoded video stream, if different from the source.
    /// </summary>
    public string? TranscodedVideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the container of the transcoded video stream, if different from the source.
    /// </summary>
    public string? TranscodedVideoContainer { get; set; }

    /// <summary>
    /// Gets or sets the codec of the transcoded audio stream, if different from the source.
    /// </summary>
    public string? TranscodedAudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the JSON-encoded reasons reported by Jellyfin for why transcoding was required.
    /// </summary>
    public string? TranscodeReasonsJson { get; set; }

    /// <summary>
    /// Gets or sets the progress of transcoding.
    /// </summary>
    public double? TranscodeCompletionPercentage { get; set; }

    /// <summary>
    /// Creates a new <see cref="PlaybackEntry"/> that represents the start of a playback span
    /// based on a Jellyfin <see cref="PlaybackStartEventArgs"/>.
    /// </summary>
    /// <param name="eventArgs">
    /// The playback start event containing media, user, client, and initial play state information.
    /// </param>
    /// <param name="imageHash">The hash of the captured image for the playback item.</param>
    /// <returns>
    /// A <see cref="PlaybackEntry"/> initialized from the event and marked as an in-progress start.
    /// </returns>
    public static PlaybackEntry FromStartEvent(PlaybackStartEventArgs eventArgs, string? imageHash)
    {
        var entry = FromEvent(eventArgs);
        // For start events, set the start time and initial position.
        entry.StartTime = DateTimeOffset.UtcNow;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        entry.StartPositionTicks = eventArgs.PlaybackPositionTicks ?? 0;
        entry.IsCompleted = false;
        entry.EndPositionTicks = eventArgs.PlaybackPositionTicks ?? 0;
        entry.IsPaused = false;
        entry.EndTime = null;
        entry.ItemImageHash = imageHash;
        return entry;
    }

    /// <summary>
    /// Creates a new <see cref="PlaybackEntry"/> snapshot that represents an in-progress playback update
    /// based on a Jellyfin <see cref="PlaybackProgressEventArgs"/>.
    /// </summary>
    /// <param name="existing">
    /// The existing <see cref="PlaybackEntry"/> to update, or null to create a new one.
    /// </param>
    /// <param name="eventArgs">
    /// The playback progress event containing the current playback position and state.
    /// </param>
    /// <param name="imageHash">
    /// The hash of the captured image for the playback item.
    /// </param>
    /// <returns>
    /// A <see cref="PlaybackEntry"/> initialized from the event and updated with the latest position.
    /// </returns>
    public static PlaybackEntry FromProgressEvent(PlaybackEntry? existing, PlaybackProgressEventArgs eventArgs, string? imageHash)
    {
        var entry = FromEvent(eventArgs);
        // For progress events, set the current position.
        entry.StartTime = existing?.StartTime ?? DateTimeOffset.UtcNow;
        entry.StartPositionTicks = existing?.StartPositionTicks ?? 0;
        entry.IsCompleted = false;
        entry.EndPositionTicks = eventArgs.PlaybackPositionTicks ?? 0;
        entry.IsPaused = eventArgs.IsPaused;
        entry.EndTime = null;
        entry.ItemImageHash = existing?.ItemImageHash ?? imageHash ?? null;
        return entry;
    }

    /// <summary>
    /// Creates a new <see cref="PlaybackEntry"/> that represents the end of a playback span
    /// based on a Jellyfin <see cref="PlaybackStopEventArgs"/>.
    /// </summary>
    /// <param name="existing">
    /// The existing <see cref="PlaybackEntry"/> to update, or null to create a new one.
    /// </param>
    /// <param name="eventArgs">
    /// The playback stop event containing the final playback position and state.
    /// </param>
    /// <param name="imageHash">
    /// The hash of the captured image for the playback item.
    /// </param>
    /// <param name="maxResumePct">
    /// The maximum resume percentage threshold from Jellyfin configuration.
    /// </param>
    /// <returns>
    /// A <see cref="PlaybackEntry"/> initialized from the event and conditionally marked as completed based on thresholds.
    /// </returns>
    public static PlaybackEntry FromStopEvent(
        PlaybackEntry? existing,
        PlaybackStopEventArgs eventArgs,
        string? imageHash,
        int? maxResumePct)
    {
        var entry = existing ?? FromEvent(eventArgs);
        // For stop events, set the end time and final position.
        entry.StartTime = existing?.StartTime ?? DateTimeOffset.UtcNow;
        entry.StartPositionTicks = existing?.StartPositionTicks ?? 0;
        var endPosition = eventArgs.PlaybackPositionTicks ?? 0;
        entry.EndPositionTicks = NormalizedEndPosition(endPosition, entry.RuntimeTicks, maxResumePct);
        entry.IsCompleted = true;
        entry.IsPaused = false;
        entry.EndTime = eventArgs.Session.LastPlaybackCheckIn;
        entry.ItemImageHash = existing?.ItemImageHash ?? imageHash ?? null;
        return entry;
    }

    /// <summary>
    /// Updates an existing <see cref="PlaybackEntry"/> to represent the end of a session based on a Jellyfin <see cref="SessionEndedEventArgs"/>.
    /// </summary>
    /// <param name="existing">The existing <see cref="PlaybackEntry"/> to update.</param>
    /// <param name="eventArgs">The session ended event containing the final playback state and timestamp.</param>
    /// <param name="maxResumePct">
    /// The maximum resume percentage threshold from Jellyfin configuration.
    /// </param>
    /// <returns>
    /// The updated <see cref="PlaybackEntry"/> conditionally marked as completed based on thresholds and with the final playback position and end time.
    /// </returns>
    public static PlaybackEntry FromSessionEndedEvent(
        PlaybackEntry existing,
        SessionEndedEventArgs eventArgs,
        int? maxResumePct)
    {
        var entry = existing;
        var endPosition = eventArgs.Argument.PlayState?.PositionTicks ?? existing.EndPositionTicks;
        entry.EndPositionTicks = NormalizedEndPosition(endPosition, entry.RuntimeTicks, maxResumePct);
        entry.IsPaused = false;
        entry.IsCompleted = true;
        entry.EndTime = eventArgs.Argument.LastPlaybackCheckIn;

        // Clear transcoding info as playback has ended. This matches the behavior in FromStopEvent.
        entry.TranscodeBitrate = null;
        entry.HardwareAcceleration = null;
        entry.TranscodedVideoCodec = null;
        entry.TranscodedVideoContainer = null;
        entry.TranscodedAudioCodec = null;
        entry.TranscodeReasonsJson = null;
        entry.TranscodeCompletionPercentage = null;
        return entry;
    }

    /// <summary>
    /// Determines whether a playback entry should be marked as completed based on Jellyfin's resume percentage thresholds.
    /// </summary>
    /// <param name="minResumePct">The minimum resume percentage threshold (0-100). If null or zero, any progress marks as completed.</param>
    /// <returns>True if the playback should be marked as completed; otherwise, false.</returns>
    /// <remarks>
    /// If RuntimeTicks is null or zero (e.g., live TV or unknown duration), always returns true.
    /// If MinResumePct is null or zero, any playback progress marks as completed.
    /// Otherwise, completion requires the actual watched duration (EndPositionTicks - StartPositionTicks) as a percentage of RuntimeTicks to be >= MinResumePct.
    /// </remarks>
    public bool ShouldTrackInHistory(int? minResumePct)
    {
        // If we don't have valid runtime, mark as completed (live TV / unknown duration).
        if (!RuntimeTicks.HasValue || RuntimeTicks.Value <= 0)
        {
            return true;
        }

        // If we don't have a valid end position, cannot be completed.
        if (!EndPositionTicks.HasValue)
        {
            return false;
        }

        // If minimum threshold is null or zero, any playback marks as completed.
        int minThreshold = minResumePct ?? 0;
        if (minThreshold <= 0)
        {
            return true;
        }

        // Calculate the actual watched duration and compare to minimum threshold.
        long watchedDuration = EndPositionTicks.Value - (StartPositionTicks ?? 0);
        double percentage = (double)watchedDuration / RuntimeTicks.Value * 100.0;
        return percentage >= minThreshold;
    }

    /// <summary>
    /// Normalizes the end position to the runtime when the maximum resume percentage threshold is exceeded.
    /// </summary>
    /// <param name="endPositionTicks">The final playback position in ticks.</param>
    /// <param name="runtimeTicks">The total runtime of the item in ticks.</param>
    /// <param name="maxResumePct">The maximum resume percentage threshold (0-100). If null or zero, no normalization occurs.</param>
    /// <returns>The normalized end position in ticks.</returns>
    /// <remarks>
    /// When playback exceeds MaxResumePct, the position is clamped to RuntimeTicks to indicate full completion.
    /// If RuntimeTicks is null or zero, returns the original EndPositionTicks.
    /// If MaxResumePct is null or zero, returns the original EndPositionTicks.
    /// </remarks>
    public static long? NormalizedEndPosition(
        long? endPositionTicks,
        long? runtimeTicks,
        int? maxResumePct)
    {
        // If we don't have valid runtime or position, return as-is.
        if (!runtimeTicks.HasValue || runtimeTicks.Value <= 0 || !endPositionTicks.HasValue)
        {
            return endPositionTicks;
        }

        // If maximum threshold is null or zero, no normalization needed.
        int maxThreshold = maxResumePct ?? 0;
        if (maxThreshold <= 0)
        {
            return endPositionTicks;
        }

        // If watch percentage exceeds maximum, clamp to runtime.
        double percentage = (double)endPositionTicks.Value / runtimeTicks.Value * 100.0;
        if (percentage >= maxThreshold)
        {
            return runtimeTicks.Value;
        }

        return endPositionTicks;
    }

    /// <summary>
    /// Generates a unique playback identifier based on the session and media information.
    /// </summary>
    /// <param name="sessionId">The Jellyfin session id.</param>
    /// <param name="playlistItemId">The Jellyfin playlist item id.</param>
    /// <param name="itemId">The Jellyfin media item id.</param>
    /// <returns>A GUID representing the generated playback identifier.</returns>
    public static Guid GeneratePlaybackId(string? sessionId, string? playlistItemId, Guid itemId)
    {
        var byt = Encoding.UTF8.GetBytes($"{sessionId}-{playlistItemId}-{itemId}");
#pragma warning disable CA5351
        var hash = MD5.HashData(byt);
#pragma warning restore CA5351
        return new Guid(hash);
    }

    /// <summary>
    /// Populates this <see cref="PlaybackEntry"/> instance from the provided <see cref="PlaybackProgressEventArgs"/>.
    /// </summary>
    /// <param name="eventArgs">The playback progress event arguments to use for populating the entry.</param>
    /// <returns>This <see cref="PlaybackEntry"/> instance after being populated from the event arguments.</returns>
    private static PlaybackEntry FromEvent(PlaybackProgressEventArgs eventArgs)
    {
        var videoStream = eventArgs.MediaInfo.MediaStreams?
            .FirstOrDefault(s => s.Type == MediaStreamType.Video);
        var audioStream = eventArgs.MediaInfo.MediaStreams?
            .FirstOrDefault(s => s.Type == MediaStreamType.Audio && s.Index == eventArgs.Session?.PlayState.AudioStreamIndex);
        var subtitleStream = eventArgs.MediaInfo.MediaStreams?
            .FirstOrDefault(s => s.Type == MediaStreamType.Subtitle && s.Index == eventArgs.Session?.PlayState.SubtitleStreamIndex);

        return new PlaybackEntry
        {
            PlaybackId = GeneratePlaybackId(eventArgs.Session.Id, eventArgs.Session.PlaylistItemId, eventArgs.MediaInfo.Id),
            SessionId = eventArgs.Session.Id,
            PlaylistItemId = eventArgs.Session.PlaylistItemId,
            ItemId = eventArgs.MediaInfo.Id,
            ParentItemId = eventArgs.MediaInfo.ParentId,
            ContentType = ContentTypeExtensions.FromBaseItemKind(eventArgs.MediaInfo.Type),
            Title = eventArgs.MediaInfo.Name,
            Genres = eventArgs.MediaInfo.Genres != null ? new Collection<string>(eventArgs.MediaInfo.Genres) : [],
            Year = eventArgs.MediaInfo.ProductionYear,
            SeriesName = eventArgs.MediaInfo.SeriesName,
            SeasonNumber = eventArgs.MediaInfo.ParentIndexNumber,
            EpisodeNumber = eventArgs.MediaInfo.IndexNumber,
            UserId = eventArgs.Users[0].Id,
            UserName = eventArgs.Users[0].Username,
            UserPrimaryImageTag = eventArgs.Session?.UserPrimaryImageTag,
            ClientName = eventArgs.ClientName,
            DeviceName = eventArgs.DeviceName,
            DeviceId = eventArgs.DeviceId,
            RuntimeTicks = eventArgs.MediaInfo.RunTimeTicks,
            VideoCodec = videoStream?.Codec,
            VideoContainer = eventArgs.MediaInfo.Container,
            VideoRange = videoStream?.VideoRange.ToString(),
            VideoBitrate = videoStream?.BitRate,
            VideoBitDepth = videoStream?.BitDepth,
            VideoHeight = videoStream?.Height,
            VideoWidth = videoStream?.Width,
            AudioLanguage = audioStream?.Language,
            AudioCodec = audioStream?.Codec,
            AudioLayout = audioStream?.ChannelLayout,
            AudioBitrate = audioStream?.BitRate,
            AudioSampleRate = audioStream?.SampleRate,
            SubtitleIsForced = subtitleStream?.IsForced,
            SubtitleIsHearingImpaired = subtitleStream?.IsHearingImpaired,
            SubtitleCodec = subtitleStream?.Codec,
            SubtitleLanguage = subtitleStream?.Language,
            IsVideoDirect = eventArgs.Session?.TranscodingInfo?.IsVideoDirect ?? true,
            IsAudioDirect = eventArgs.Session?.TranscodingInfo?.IsAudioDirect ?? true,
            TranscodeBitrate = eventArgs.Session?.TranscodingInfo?.Bitrate,
            HardwareAcceleration = eventArgs.Session?.TranscodingInfo?.HardwareAccelerationType?.ToString(),
            TranscodedVideoCodec = eventArgs.Session?.TranscodingInfo?.VideoCodec,
            TranscodedVideoContainer = eventArgs.Session?.TranscodingInfo?.Container,
            TranscodedAudioCodec = eventArgs.Session?.TranscodingInfo?.AudioCodec,
            TranscodeReasonsJson = System.Text.Json.JsonSerializer.Serialize(eventArgs.Session?.TranscodingInfo?.TranscodeReasons),
            TranscodeCompletionPercentage = eventArgs.Session?.TranscodingInfo?.CompletionPercentage
        };
    }
}
