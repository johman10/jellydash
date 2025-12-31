using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Configuration;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Jellydash.ScheduledTasks;

/// <summary>
/// Scheduled task that prunes historical Jellydash entries older than the configured retention window.
/// </summary>
public sealed class JellydashHistoryCleanupTask : IScheduledTask
{
    private readonly HistoryRepository _historyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellydashHistoryCleanupTask"/> class.
    /// </summary>
    /// <param name="historyRepository">The history repository.</param>
    public JellydashHistoryCleanupTask(HistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    /// <inheritdoc />
    public string Name => "Jellydash history cleanup";

    /// <inheritdoc />
    public string Key => "JellydashHistoryCleanup";

    /// <inheritdoc />
    public string Description => "Removes Jellydash history entries older than the configured retention window.";

    /// <inheritdoc />
    public string Category => "Jellydash";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);

        var plugin = Plugin.Instance;
        var config = plugin?.Configuration;

        if (config is null)
        {
            // Nothing to do without configuration; report as complete.
            progress.Report(1);
            return;
        }

        if (!config.EnableRetention)
        {
            // Retention-based cleanup is disabled by configuration.
            progress.Report(1);
            return;
        }

        var retentionDays = config.HistoryRetentionDays;
        if (retentionDays <= 0)
        {
            retentionDays = 30;
        }

        var retention = TimeSpan.FromDays(retentionDays);
        var cutoffUtc = DateTime.UtcNow - retention;

        var removed = await _historyRepository
            .DeleteOlderThanAsync(cutoffUtc, cancellationToken)
            .ConfigureAwait(false);

        // Simple progress model: once delete returns, task is done.
        progress.Report(1);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Run once per day around 03:00 server time by default.
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        ];
    }
}
