using System;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents a unified view of a playback span used by Jellydash.
/// </summary>
public class PlaybackEntryDto(Guid itemId, Guid? parentItemId, ContentKind contentKind, ContentIdentityDto identity, UserInfoDto user, ClientInfoDto client, TimingInfoDto timing, StreamInfoDto streams, TranscodingInfoDto? transcoding, bool isCompleted, bool isPaused)
{
    /// <summary>
    /// Gets the Jellyfin item identifier.
    /// </summary>
    public Guid ItemId { get; init; } = itemId;

    /// <summary>
    /// Gets the Jellyfin parent item identifier.
    /// Only relevant for episodes within a series.
    /// </summary>
    public Guid? ParentItemId { get; init; } = parentItemId;

    /// <summary>
    /// Gets the kind of content (movie, episode, etc.).
    /// </summary>
    public ContentKind ContentKind { get; init; } = contentKind;

    /// <summary>
    /// Gets the identity metadata for the content.
    /// </summary>
    public ContentIdentityDto Identity { get; init; } = identity;

    /// <summary>
    /// Gets information about the user associated with this playback.
    /// </summary>
    public UserInfoDto User { get; init; } = user;

    /// <summary>
    /// Gets information about the client and device.
    /// </summary>
    public ClientInfoDto Client { get; init; } = client;

    /// <summary>
    /// Gets timing and progress information for this playback.
    /// </summary>
    public TimingInfoDto Timing { get; init; } = timing;

    /// <summary>
    /// Gets technical stream information for this playback.
    /// </summary>
    public StreamInfoDto Streams { get; init; } = streams;

    /// <summary>
    /// Gets transcoding-specific information, if any.
    /// </summary>
    public TranscodingInfoDto? Transcoding { get; init; } = transcoding;

    /// <summary>
    /// Gets a value indicating whether the playback has fully completed.
    /// </summary>
    public bool IsCompleted { get; init; } = isCompleted;

    /// <summary>
    /// Gets a value indicating whether playback is currently paused.
    /// </summary>
    public bool IsPaused { get; init; } = isPaused;
}
