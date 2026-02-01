using System.Collections.ObjectModel;

namespace Jellyfin.Plugin.Jellydash.Models
{
    /// <summary>
    /// Represents a paginated response containing playback history entries and a cursor for the next page.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ActivityResponse"/> class.
    /// </remarks>
    /// <param name="items">The list of playback entries.</param>
    /// <param name="nextCursor">The cursor for the next page, if any.</param>
    public class ActivityResponse(Collection<PlaybackEntryDto> items, string? nextCursor)
    {
        /// <summary>
        /// Gets the list of playback entries returned in this response.
        /// </summary>
        public Collection<PlaybackEntryDto> Items { get; private set; } = items;

        /// <summary>
        /// Gets the cursor for the next page of results, if any.
        /// </summary>
        public string? NextCursor { get; private set; } = nextCursor;
    }
}
