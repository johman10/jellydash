namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents technical information about a subtitle track.
/// </summary>
public class SubtitleTrackDto
{
    /// <summary>
    /// Gets a value indicating whether the subtitle track is forced.
    /// </summary>
    public bool IsForced { get; init; }

    /// <summary>
    /// Gets a value indicating whether the subtitle track is for the hearing impaired.
    /// </summary>
    public bool IsHearingImpaired { get; init; }

    /// <summary>
    /// Gets the subtitle codec.
    /// </summary>
    public string? Codec { get; init; }

    /// <summary>
    /// Gets the language of the subtitle track.
    /// </summary>
    public string? Language { get; init; }
}
