using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Jellydash.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ActivityRetentionDays = 30;
        EnableRetention = true;
        TrackDownloads = true;
    }

    /// <summary>
    /// Gets or sets the activity retention period in days.
    /// </summary>
    public int ActivityRetentionDays { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether retention-based cleanup is enabled.
    /// </summary>
    public bool EnableRetention { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether download activity is tracked.
    /// </summary>
    public bool TrackDownloads { get; set; }
}
