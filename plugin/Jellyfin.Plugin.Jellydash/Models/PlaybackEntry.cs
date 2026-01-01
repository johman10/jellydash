using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents a single contiguous playback span for a media item as stored in SQLite.
/// </summary>
public class PlaybackEntry
{
    // Identity

    /// <summary>
    /// Gets or sets the Jellyfin item identifier.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the kind of content (movie, episode, or other).
    /// </summary>
    public ContentKind ContentKind { get; set; }

    /// <summary>
    /// Gets or sets the primary display title (series or movie name).
    /// </summary>
    public string DisplayTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL or path to the primary image.
    /// </summary>
    public string? PrimaryImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the primary genre of the item, if known.
    /// </summary>
    public string? PrimaryGenre { get; set; }

    /// <summary>
    /// Gets or sets the production year when the content is a movie.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the series name when the content is an episode.
    /// </summary>
    public string? SeriesName { get; set; }

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
    /// Gets or sets the URL or path to the user's primary image.
    /// </summary>
    public string? UserImageUrl { get; set; }

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
    public DateTime StartUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this span ended, if completed.
    /// </summary>
    public DateTime? EndUtc { get; set; }

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
    /// Gets or sets the starting watched percentage (0-100), if known.
    /// </summary>
    public double? StartPercentage { get; set; }

    /// <summary>
    /// Gets or sets the ending watched percentage (0-100), if known.
    /// </summary>
    public double? EndPercentage { get; set; }

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
    /// Gets or sets the codec of the transcoded audio stream, if different from the source.
    /// </summary>
    public string? TranscodedAudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the JSON-encoded reasons reported by Jellyfin for why transcoding was required.
    /// </summary>
    public string? TranscodeReasonsJson { get; set; }

    /// <summary>
    /// Converts this stored playback entry into an API-facing <see cref="PlaybackEntryDto"/>.
    /// </summary>
    /// <returns>A new <see cref="PlaybackEntryDto"/> instance populated from this entry.</returns>
    public PlaybackEntryDto ToDto()
    {
        var identity = new ContentIdentityDto
        {
            DisplayTitle = DisplayTitle,
            PrimaryImageUrl = PrimaryImageUrl,
            PrimaryGenre = PrimaryGenre,
            Year = Year,
            SeriesName = SeriesName,
            SeasonNumber = SeasonNumber,
            EpisodeNumber = EpisodeNumber
        };

        var user = new UserInfoDto
        {
            UserId = UserId,
            UserName = UserName,
            UserImageUrl = UserImageUrl
        };

        var client = new ClientInfoDto
        {
            ClientName = ClientName,
            DeviceName = DeviceName,
            DeviceId = DeviceId
        };

        var timing = new TimingInfoDto
        {
            StartUtc = StartUtc,
            EndUtc = EndUtc,
            RuntimeTicks = RuntimeTicks,
            StartPositionTicks = StartPositionTicks,
            EndPositionTicks = EndPositionTicks,
            StartPercentage = StartPercentage,
            EndPercentage = EndPercentage
        };

        VideoTrackDto? video = null;
        if (VideoCodec is not null
            || VideoContainer is not null
            || VideoRange is not null
            || VideoBitrate.HasValue
            || VideoBitDepth.HasValue
            || VideoHeight.HasValue
            || VideoWidth.HasValue)
        {
            video = new VideoTrackDto
            {
                Codec = VideoCodec,
                Container = VideoContainer,
                VideoRange = VideoRange,
                Bitrate = VideoBitrate,
                BitDepth = VideoBitDepth,
                Height = VideoHeight,
                Width = VideoWidth
            };
        }

        AudioTrackDto? audio = null;
        if (AudioLanguage is not null
            || AudioCodec is not null
            || AudioLayout is not null
            || AudioBitrate.HasValue
            || AudioSampleRate.HasValue)
        {
            audio = new AudioTrackDto
            {
                Language = AudioLanguage,
                Codec = AudioCodec,
                Layout = AudioLayout,
                Bitrate = AudioBitrate,
                SampleRate = AudioSampleRate
            };
        }

        SubtitleTrackDto? subtitle = null;
        if (SubtitleIsForced.HasValue
            || SubtitleIsHearingImpaired.HasValue
            || SubtitleCodec is not null
            || SubtitleLanguage is not null)
        {
            subtitle = new SubtitleTrackDto
            {
                IsForced = SubtitleIsForced ?? false,
                IsHearingImpaired = SubtitleIsHearingImpaired ?? false,
                Codec = SubtitleCodec,
                Language = SubtitleLanguage
            };
        }

        var streams = new StreamInfoDto
        {
            Video = video,
            Audio = audio,
            Subtitle = subtitle
        };

        TranscodingInfoDto? transcoding = null;
        if (!IsVideoDirect
            || !IsAudioDirect
            || HardwareAcceleration is not null
            || TranscodeBitrate.HasValue
            || TranscodedVideoCodec is not null
            || TranscodedAudioCodec is not null
            || !string.IsNullOrEmpty(TranscodeReasonsJson))
        {
            IReadOnlyList<string> reasons;
            if (string.IsNullOrWhiteSpace(TranscodeReasonsJson))
            {
                reasons = Array.Empty<string>();
            }
            else
            {
                // The JSON is stored as a simple string array; parse conservatively.
                try
                {
                    reasons = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<string>>(TranscodeReasonsJson!)
                              ?? Array.Empty<string>();
                }
                catch
                {
                    reasons = Array.Empty<string>();
                }
            }

            VideoTrackDto? transcodedVideo = null;
            if (TranscodedVideoCodec is not null)
            {
                transcodedVideo = new VideoTrackDto
                {
                    Codec = TranscodedVideoCodec,
                    Bitrate = TranscodeBitrate
                };
            }

            AudioTrackDto? transcodedAudio = null;
            if (TranscodedAudioCodec is not null)
            {
                transcodedAudio = new AudioTrackDto
                {
                    Codec = TranscodedAudioCodec,
                    Bitrate = TranscodeBitrate
                };
            }

            transcoding = new TranscodingInfoDto
            {
                IsVideoDirect = IsVideoDirect,
                IsAudioDirect = IsAudioDirect,
                HardwareAcceleration = HardwareAcceleration,
                Bitrate = TranscodeBitrate,
                TranscodedVideo = transcodedVideo,
                TranscodedAudio = transcodedAudio,
                Reasons = reasons
            };
        }

        return new PlaybackEntryDto
        {
            ItemId = ItemId,
            ContentKind = ContentKind,
            Identity = identity,
            User = user,
            Client = client,
            Timing = timing,
            Streams = streams,
            Transcoding = transcoding,
            IsCompleted = IsCompleted,
            IsPaused = IsPaused
        };
    }
}
