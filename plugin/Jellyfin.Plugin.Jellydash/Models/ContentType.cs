using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Describes the kind of media content represented by a playback entry.
/// </summary>
public enum ContentType
{
    /// <summary>
    /// A movie or other single-item video.
    /// </summary>
    [EnumMember(Value = "Movie")]
    Movie,

    /// <summary>
    /// A TV episode.
    /// </summary>
    [EnumMember(Value = "Episode")]
    Episode,

    /// <summary>
    /// Any other content type not explicitly handled.
    /// </summary>
    [EnumMember(Value = "Other")]
    Other,
}
