using System;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents summary information about a Jellyfin user.
/// </summary>
public class UserInfoDto(string? userPrimaryImageTag)
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
    /// Gets the relative path to the user's primary image.
    /// </summary>
    public string? UserImagePath
    {
        get
        {
            if (UserPrimaryImageTag != null)
            {
                return $"/Users/{UserId}/Images/Primary?tag={UserPrimaryImageTag}";
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the primary image tag for the user, used to generate the image path.
    /// </summary>
    private string? UserPrimaryImageTag { get; init; } = userPrimaryImageTag;
}
