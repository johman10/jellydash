using System;

namespace Jellyfin.Plugin.Jellydash.Models;

/// <summary>
/// Represents timing and progress information for a playback span.
/// </summary>
public class TimingInfoDto
{
    /// <summary>
    /// Gets the UTC timestamp when playback started.
    /// </summary>
    public DateTime StartUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when playback ended, if completed.
    /// </summary>
    public DateTime? EndUtc { get; init; }

    /// <summary>
    /// Gets the total runtime of the item in ticks.
    /// </summary>
    public long? RuntimeTicks { get; init; }

    /// <summary>
    /// Gets the starting playback position in ticks.
    /// </summary>
    public long? StartPositionTicks { get; init; }

    /// <summary>
    /// Gets the ending playback position in ticks, if completed.
    /// </summary>
    public long? EndPositionTicks { get; init; }

    /// <summary>
    /// Gets the starting watched percentage (0-100), if known.
    /// </summary>
    public double? StartPercentage
    {
        get
        {
            return StartPositionTicks.HasValue && RuntimeTicks.HasValue && RuntimeTicks.Value > 0
                ? (double)StartPositionTicks.Value / RuntimeTicks.Value * 100.0
                : null;
        }
    }

    /// <summary>
    /// Gets the ending watched percentage (0-100), if known.
    /// </summary>
    public double? EndPercentage
    {
        get
        {
            return EndPositionTicks.HasValue && RuntimeTicks.HasValue && RuntimeTicks.Value > 0
                ? (double)EndPositionTicks.Value / RuntimeTicks.Value * 100.0
                : null;
        }
    }
}
