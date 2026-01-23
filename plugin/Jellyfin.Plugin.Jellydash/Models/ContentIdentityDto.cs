using System;
using System.Collections.ObjectModel;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents the identity and display metadata for a media item.
/// </summary>
public class ContentIdentityDto(ContentType contentType, Guid itemId, Guid? parentItemId)
{
    /// <summary>
    /// Gets the relative path to the primary image.
    /// </summary>
    public string? PrimaryImagePath
    {
        get
        {
            if (contentType == ContentType.Episode)
            {
                return $"/Items/{parentItemId}/Images/Primary";
            }
            else
            {
                return $"/Items/{itemId}/Images/Primary";
            }
        }
    }

    /// <summary>
    /// Gets the primary display title (episode or movie name).
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the primary genre of the item, if known.
    /// </summary>
    public Collection<string> Genres { get; init; } = new Collection<string>();

    /// <summary>
    /// Gets the production year when the content is a movie.
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Gets the series name when the content is an episode.
    /// </summary>
    public string? SeriesName { get; init; }

    /// <summary>
    /// Gets the season number when the content is an episode.
    /// </summary>
    public int? SeasonNumber { get; init; }

    /// <summary>
    /// Gets the episode number when the content is an episode.
    /// </summary>
    public int? EpisodeNumber { get; init; }
}
