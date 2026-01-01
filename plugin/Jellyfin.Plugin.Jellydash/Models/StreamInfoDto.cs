namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents the selected media streams for a playback.
/// </summary>
public class StreamInfoDto
{
    /// <summary>
    /// Gets information about the video track, if any.
    /// </summary>
    public VideoTrackDto? Video { get; init; }

    /// <summary>
    /// Gets information about the audio track, if any.
    /// </summary>
    public AudioTrackDto? Audio { get; init; }

    /// <summary>
    /// Gets information about the subtitle track, if any.
    /// </summary>
    public SubtitleTrackDto? Subtitle { get; init; }
}
