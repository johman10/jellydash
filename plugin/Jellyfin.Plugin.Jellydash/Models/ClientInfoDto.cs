namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents information about the client application and device.
/// </summary>
public class ClientInfoDto
{
    /// <summary>
    /// Gets the client application name.
    /// </summary>
    public string ClientName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the device name.
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional device identifier.
    /// </summary>
    public string? DeviceId { get; init; }
}
