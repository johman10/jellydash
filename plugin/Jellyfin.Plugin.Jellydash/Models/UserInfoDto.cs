using System;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents summary information about a Jellyfin user.
/// </summary>
public class UserInfoDto
{
    /// <summary>
    /// Gets the Jellyfin user identifier.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the user name for display.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the URL or path to the user's primary image.
    /// </summary>
    public string? UserImageUrl { get; init; }
}
