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
        RetentionDays = 30;
        EnableRetention = true;
    }

    /// <summary>
    /// Gets or sets the playback entry retention period in days.
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether retention-based cleanup is enabled.
    /// </summary>
    public bool EnableRetention { get; set; }
}
