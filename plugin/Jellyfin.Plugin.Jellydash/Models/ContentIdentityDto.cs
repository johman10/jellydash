namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents the identity and display metadata for a media item.
/// </summary>
public class ContentIdentityDto
{
    /// <summary>
    /// Gets the primary display title (series or movie name).
    /// </summary>
    public string DisplayTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets the URL or path to the primary image.
    /// </summary>
    public string? PrimaryImageUrl { get; init; }

    /// <summary>
    /// Gets the primary genre of the item, if known.
    /// </summary>
    public string? PrimaryGenre { get; init; }

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
