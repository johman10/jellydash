namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents technical information about an audio track.
/// </summary>
public class AudioTrackDto
{
    /// <summary>
    /// Gets the language of the audio track.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the audio codec (for example, ac3, eac3, dts).
    /// </summary>
    public string? Codec { get; init; }

    /// <summary>
    /// Gets the channel layout (for example, 2.0, 5.1).
    /// </summary>
    public string? Layout { get; init; }

    /// <summary>
    /// Gets the approximate bitrate in bits per second.
    /// </summary>
    public int? Bitrate { get; init; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    public int? SampleRate { get; init; }
}
