using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents transcoding-specific information layered on top of stream info.
/// </summary>
public class TranscodingInfoDto
{
    /// <summary>
    /// Gets a value indicating whether the video stream is direct.
    /// </summary>
    public bool IsVideoDirect { get; init; }

    /// <summary>
    /// Gets a value indicating whether the audio stream is direct.
    /// </summary>
    public bool IsAudioDirect { get; init; }

    /// <summary>
    /// Gets the hardware acceleration mode, if any.
    /// </summary>
    public string? HardwareAcceleration { get; init; }

    /// <summary>
    /// Gets the overall transcoded bitrate in bits per second, if known.
    /// </summary>
    public int? Bitrate { get; init; }

    /// <summary>
    /// Gets the transcoded video track details, if different from the source.
    /// </summary>
    public VideoTrackDto? TranscodedVideo { get; init; }

    /// <summary>
    /// Gets the transcoded audio track details, if different from the source.
    /// </summary>
    public AudioTrackDto? TranscodedAudio { get; init; }

    /// <summary>
    /// Gets reasons reported by Jellyfin for why transcoding was required.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the completion percentage of the transcoding process, if known.
    /// </summary>
    public double? CompletionPercentage { get; init; }
}
