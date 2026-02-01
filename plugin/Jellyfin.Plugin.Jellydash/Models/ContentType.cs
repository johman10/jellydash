using System.Runtime.Serialization;
using Jellyfin.Data.Enums;

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

#pragma warning disable SA1649 // File name should match first type name
/// <summary>
/// Extension methods for the <see cref="ContentType"/> enum.
/// </summary>
public static class ContentTypeExtensions
#pragma warning restore SA1649 // File name should match first type name
{
    /// <summary>
    /// Maps a <see cref="BaseItemKind"/> to a <see cref="ContentType"/>.
    /// </summary>
    /// <param name="itemKind">The base item kind to map.</param>
    /// <returns>The corresponding <see cref="ContentType"/> value.</returns>
    public static ContentType FromBaseItemKind(this BaseItemKind itemKind)
    {
        return itemKind switch
        {
            BaseItemKind.Movie => ContentType.Movie,
            BaseItemKind.Episode => ContentType.Episode,
            _ => ContentType.Other
        };
    }
}
