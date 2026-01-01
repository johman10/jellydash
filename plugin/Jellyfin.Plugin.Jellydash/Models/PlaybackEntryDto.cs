using System;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents a unified view of a playback span used by Jellydash.
/// </summary>
public class PlaybackEntryDto
{
    /// <summary>
    /// Gets the Jellyfin item identifier.
    /// </summary>
    public Guid ItemId { get; init; }

    /// <summary>
    /// Gets the kind of content (movie, episode, etc.).
    /// </summary>
    public ContentKind ContentKind { get; init; }

    /// <summary>
    /// Gets the identity metadata for the content.
    /// </summary>
    public ContentIdentityDto Identity { get; init; } = new();

    /// <summary>
    /// Gets information about the user associated with this playback.
    /// </summary>
    public UserInfoDto User { get; init; } = new();

    /// <summary>
    /// Gets information about the client and device.
    /// </summary>
    public ClientInfoDto Client { get; init; } = new();

    /// <summary>
    /// Gets timing and progress information for this playback.
    /// </summary>
    public TimingInfoDto Timing { get; init; } = new();

    /// <summary>
    /// Gets technical stream information for this playback.
    /// </summary>
    public StreamInfoDto Streams { get; init; } = new();

    /// <summary>
    /// Gets transcoding-specific information, if any.
    /// </summary>
    public TranscodingInfoDto? Transcoding { get; init; }

    /// <summary>
    /// Gets a value indicating whether the playback has fully completed.
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Gets a value indicating whether playback is currently paused.
    /// </summary>
    public bool IsPaused { get; init; }
}
