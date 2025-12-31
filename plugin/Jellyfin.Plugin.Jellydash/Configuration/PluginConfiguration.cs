using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Jellydash.Configuration;

/// <summary>
/// Units for the history retention window.
/// </summary>
public enum RetentionUnit
{
    /// <summary>
    /// Retain history for a number of hours.
    /// </summary>
    Hours,

    /// <summary>
    /// Retain history for a number of days.
    /// </summary>
    Days
}

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
        HistoryRetentionValue = 7;
        HistoryRetentionUnit = RetentionUnit.Days;
        TrackDownloads = true;
    }

    /// <summary>
    /// Gets or sets the numeric value for the history retention window.
    /// </summary>
    public int HistoryRetentionValue { get; set; }

    /// <summary>
    /// Gets or sets the unit for the history retention window.
    /// </summary>
    public RetentionUnit HistoryRetentionUnit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether download activity is tracked.
    /// </summary>
    public bool TrackDownloads { get; set; }
}
