using System;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents a single contiguous playback or download span for a media item.
/// </summary>
public class Activity
{
    /// <summary>
    /// Gets or sets the Jellyfin user id associated with this entry.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name for display.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL (or path) to the user's primary image.
    /// </summary>
    public string? UserImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item id.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the client application name.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the media type (e.g. Movie, Episode).
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item name (series or movie name).
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the series name when the item is an episode.
    /// </summary>
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the season number when the item is an episode.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number when the item is an episode.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the production year of the item.
    /// </summary>
    public int? ProductionYear { get; set; }

    /// <summary>
    /// Gets or sets the primary genre of the item, if known.
    /// </summary>
    public string? PrimaryGenre { get; set; }

    /// <summary>
    /// Gets or sets the primary image path or URL for the item.
    /// </summary>
    public string? PrimaryImagePath { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this span started.
    /// </summary>
    public DateTime StartUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this span ended.
    /// </summary>
    public DateTime EndUtc { get; set; }

    /// <summary>
    /// Gets or sets the total runtime of the item in ticks.
    /// </summary>
    public long? RuntimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the starting position of this span in ticks.
    /// </summary>
    public long? StartPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the ending position of this span in ticks.
    /// </summary>
    public long? EndPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the starting watched percentage (0-100).
    /// </summary>
    public double StartPercentage { get; set; }

    /// <summary>
    /// Gets or sets the ending watched percentage (0-100).
    /// </summary>
    public double EndPercentage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry represents a download activity.
    /// </summary>
    public bool IsDownload { get; set; }

    /// <summary>
    /// Gets or sets the total download size in bytes, if applicable.
    /// </summary>
    public long? DownloadSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the downloaded bytes at the end of this span, if applicable.
    /// </summary>
    public long? DownloadedBytes { get; set; }

    /// <summary>
    /// Gets or sets the download progress percentage (0-100) at the end of this span.
    /// </summary>
    public double? DownloadProgressPercent { get; set; }

    /// <summary>
    /// Gets or sets the overall bitrate in bits per second, if known.
    /// </summary>
    public long? Bitrate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video stream was direct during this span.
    /// </summary>
    public bool IsVideoDirectStream { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio stream was direct during this span.
    /// </summary>
    public bool IsAudioDirectStream { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this span was transcoded.
    /// </summary>
    public bool IsTranscoding { get; set; }

    /// <summary>
    /// Gets or sets the video codec used during this span when transcoding.
    /// </summary>
    public string TranscodeVideoCodec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audio codec used during this span when transcoding.
    /// </summary>
    public string TranscodeAudioCodec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container format used during this span when transcoding.
    /// </summary>
    public string TranscodeContainer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hardware acceleration mode used, if any.
    /// </summary>
    public string TranscodeHardwareAcceleration { get; set; } = string.Empty;
}
