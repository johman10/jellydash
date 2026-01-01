namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Describes the kind of media content represented by a playback entry.
/// </summary>
public enum ContentKind
{
    /// <summary>
    /// A movie or other single-item video.
    /// </summary>
    Movie,

    /// <summary>
    /// A TV episode.
    /// </summary>
    Episode,

    /// <summary>
    /// Any other content type not explicitly handled.
    /// </summary>
    Other,
}
