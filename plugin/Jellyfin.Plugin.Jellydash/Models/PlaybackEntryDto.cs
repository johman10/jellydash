using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents a unified view of a playback span used by Jellydash.
/// </summary>
public class PlaybackEntryDto(Guid itemId, Guid? parentItemId, ContentType contentType, ContentIdentityDto identity, UserInfoDto user, ClientInfoDto client, TimingInfoDto timing, StreamInfoDto streams, TranscodingInfoDto? transcoding, bool isCompleted, bool isPaused)
{
    /// <summary>
    /// Gets the Jellyfin item identifier.
    /// </summary>
    public Guid ItemId { get; init; } = itemId;

    /// <summary>
    /// Gets the Jellyfin parent item identifier.
    /// Only relevant for episodes within a series.
    /// </summary>
    public Guid? ParentItemId { get; init; } = parentItemId;

    /// <summary>
    /// Gets the kind of content (movie, episode, etc.).
    /// </summary>
    public ContentType ContentType { get; init; } = contentType;

    /// <summary>
    /// Gets the identity metadata for the content.
    /// </summary>
    public ContentIdentityDto Identity { get; init; } = identity;

    /// <summary>
    /// Gets information about the user associated with this playback.
    /// </summary>
    public UserInfoDto User { get; init; } = user;

    /// <summary>
    /// Gets information about the client and device.
    /// </summary>
    public ClientInfoDto Client { get; init; } = client;

    /// <summary>
    /// Gets timing and progress information for this playback.
    /// </summary>
    public TimingInfoDto Timing { get; init; } = timing;

    /// <summary>
    /// Gets technical stream information for this playback.
    /// </summary>
    public StreamInfoDto Streams { get; init; } = streams;

    /// <summary>
    /// Gets transcoding-specific information, if any.
    /// </summary>
    public TranscodingInfoDto? Transcoding { get; init; } = transcoding;

    /// <summary>
    /// Gets a value indicating whether the playback has fully completed.
    /// </summary>
    public bool IsCompleted { get; init; } = isCompleted;

    /// <summary>
    /// Gets a value indicating whether playback is currently paused.
    /// </summary>
    public bool IsPaused { get; init; } = isPaused;

    /// <summary>
    /// Converts the stored <see cref="PlaybackEntry"/> into an API-facing <see cref="PlaybackEntryDto"/>.
    /// </summary>
    /// <param name="entry">The playback entry to convert.</param>
    /// <returns>A new <see cref="PlaybackEntryDto"/> instance populated from this entry.</returns>
    public static PlaybackEntryDto FromPlaybackEntry(PlaybackEntry entry)
    {
        var identity = new ContentIdentityDto(contentType: entry.ContentType, itemId: entry.ItemId, parentItemId: entry.ParentItemId, itemImageHash: entry.ItemImageHash)
        {
            Title = entry.Title,
            Genres = entry.Genres,
            Year = entry.Year,
            SeriesName = entry.SeriesName,
            SeasonNumber = entry.SeasonNumber,
            EpisodeNumber = entry.EpisodeNumber
        };

        var user = new UserInfoDto(entry.UserPrimaryImageTag)
        {
            UserId = entry.UserId,
            UserName = entry.UserName,
        };

        var client = new ClientInfoDto
        {
            ClientName = entry.ClientName,
            DeviceName = entry.DeviceName,
            DeviceId = entry.DeviceId
        };

        var timing = new TimingInfoDto
        {
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            RuntimeTicks = entry.RuntimeTicks,
            StartPositionTicks = entry.StartPositionTicks,
            EndPositionTicks = entry.EndPositionTicks,
        };

        VideoTrackDto? video = null;
        if (entry.VideoCodec is not null
            || entry.VideoContainer is not null
            || entry.VideoRange is not null
            || entry.VideoBitrate.HasValue
            || entry.VideoBitDepth.HasValue
            || entry.VideoHeight.HasValue
            || entry.VideoWidth.HasValue)
        {
            video = new VideoTrackDto
            {
                Codec = entry.VideoCodec,
                Container = entry.VideoContainer,
                VideoRange = entry.VideoRange,
                Bitrate = entry.VideoBitrate,
                BitDepth = entry.VideoBitDepth,
                Height = entry.VideoHeight,
                Width = entry.VideoWidth
            };
        }

        AudioTrackDto? audio = null;
        if (entry.AudioLanguage is not null
            || entry.AudioCodec is not null
            || entry.AudioLayout is not null
            || entry.AudioBitrate.HasValue
            || entry.AudioSampleRate.HasValue)
        {
            audio = new AudioTrackDto
            {
                Language = entry.AudioLanguage,
                Codec = entry.AudioCodec,
                Layout = entry.AudioLayout,
                Bitrate = entry.AudioBitrate,
                SampleRate = entry.AudioSampleRate
            };
        }

        SubtitleTrackDto? subtitle = null;
        if (entry.SubtitleIsForced.HasValue
            || entry.SubtitleIsHearingImpaired.HasValue
            || entry.SubtitleCodec is not null
            || entry.SubtitleLanguage is not null)
        {
            subtitle = new SubtitleTrackDto
            {
                IsForced = entry.SubtitleIsForced ?? false,
                IsHearingImpaired = entry.SubtitleIsHearingImpaired ?? false,
                Codec = entry.SubtitleCodec,
                Language = entry.SubtitleLanguage
            };
        }

        var streams = new StreamInfoDto
        {
            Video = video,
            Audio = audio,
            Subtitle = subtitle
        };

        TranscodingInfoDto? transcoding = null;
        if (!entry.IsVideoDirect
            || !entry.IsAudioDirect
            || entry.HardwareAcceleration is not null
            || entry.TranscodeBitrate.HasValue
            || entry.TranscodedVideoCodec is not null
            || entry.TranscodedVideoContainer is not null
            || entry.TranscodedAudioCodec is not null
            || !string.IsNullOrEmpty(entry.TranscodeReasonsJson)
            || entry.TranscodeCompletionPercentage.HasValue)
        {
            IReadOnlyList<string> reasons;
            if (string.IsNullOrWhiteSpace(entry.TranscodeReasonsJson))
            {
                reasons = [];
            }
            else
            {
                // The JSON is stored as a simple string array; parse conservatively.
                try
                {
                    reasons = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<string>>(entry.TranscodeReasonsJson!)
                                ?? [];
                }
                catch
                {
                    reasons = [];
                }
            }

            VideoTrackDto? transcodedVideo = null;
            if (entry.TranscodedVideoContainer is not null || entry.TranscodedVideoCodec is not null || entry.TranscodeBitrate.HasValue)
            {
                transcodedVideo = new VideoTrackDto
                {
                    Container = entry.TranscodedVideoContainer,
                    Codec = entry.TranscodedVideoCodec,
                    Bitrate = entry.TranscodeBitrate
                };
            }

            AudioTrackDto? transcodedAudio = null;
            if (entry.TranscodedAudioCodec is not null)
            {
                transcodedAudio = new AudioTrackDto
                {
                    Codec = entry.TranscodedAudioCodec,
                    Bitrate = entry.TranscodeBitrate
                };
            }

            transcoding = new TranscodingInfoDto
            {
                IsVideoDirect = entry.IsVideoDirect,
                IsAudioDirect = entry.IsAudioDirect,
                HardwareAcceleration = entry.HardwareAcceleration,
                Bitrate = entry.TranscodeBitrate,
                TranscodedVideo = transcodedVideo,
                TranscodedAudio = transcodedAudio,
                Reasons = reasons,
                CompletionPercentage = entry.TranscodeCompletionPercentage
            };
        }

        return new PlaybackEntryDto(
            itemId: entry.ItemId,
            parentItemId: entry.ParentItemId,
            contentType: entry.ContentType,
            identity: identity,
            user: user,
            client: client,
            timing: timing,
            streams: streams,
            transcoding: transcoding,
            isCompleted: entry.IsCompleted,
            isPaused: entry.IsPaused);
    }
}
