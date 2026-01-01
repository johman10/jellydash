namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents technical information about a video track.
/// </summary>
public class VideoTrackDto
{
    /// <summary>
    /// Gets the video codec (for example, h264, hevc).
    /// </summary>
    public string? Codec { get; init; }

    /// <summary>
    /// Gets the container format.
    /// </summary>
    public string? Container { get; init; }

    /// <summary>
    /// Gets the video range (for example, SDR, HDR10, DolbyVision).
    /// </summary>
    public string? VideoRange { get; init; }

    /// <summary>
    /// Gets the approximate bitrate in bits per second.
    /// </summary>
    public int? Bitrate { get; init; }

    /// <summary>
    /// Gets the bit depth of the video.
    /// </summary>
    public int? BitDepth { get; init; }

    /// <summary>
    /// Gets the encoded video height in pixels.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Gets the encoded video width in pixels.
    /// </summary>
    public int? Width { get; init; }
}
