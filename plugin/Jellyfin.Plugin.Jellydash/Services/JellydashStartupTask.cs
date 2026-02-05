using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Models;
using Jellyfin.Plugin.Jellydash.Services;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellydash.Services;

/// <summary>
/// Performs startup initialization for the Jellydash plugin.
/// </summary>
public sealed class JellydashStartupTask : IHostedService
{
    private readonly ILogger<JellydashStartupTask> _logger;
    private readonly PlaybackEntryRepository _repository;
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellydashStartupTask"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="repository">Playback entry repository.</param>
    /// <param name="configurationManager">Server configuration manager for accessing resume percentage thresholds.</param>
    public JellydashStartupTask(
        ILogger<JellydashStartupTask> logger,
        PlaybackEntryRepository repository,
        IServerConfigurationManager configurationManager)
    {
        _logger = logger;
        _repository = repository;
        _configurationManager = configurationManager;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running Jellydash startup cleanup...");
            var minResumePct = _configurationManager.Configuration.MinResumePct;

            var incompleteEntries = await _repository.GetAllIncompleteEntriesAsync(cancellationToken).ConfigureAwait(false);

            int completedCount = 0;
            int deletedCount = 0;

            foreach (var entry in incompleteEntries)
            {
                // Temporarily set EndTime to UpdatedAt and EndPositionTicks if needed to check threshold
                entry.EndTime = entry.UpdatedAt;
                if (entry.EndPositionTicks is null)
                {
                    entry.EndPositionTicks = entry.StartPositionTicks;
                }

                if (entry.ShouldTrackInHistory(minResumePct))
                {
                    // Mark as completed - manipulate the entry and upsert
                    entry.IsCompleted = true;
                    entry.IsPaused = false;
                    entry.EndTime = entry.UpdatedAt;
                    entry.EndPositionTicks = PlaybackEntry.NormalizedEndPosition(entry.EndPositionTicks ?? entry.StartPositionTicks, entry.RuntimeTicks, minResumePct);

                    // Clear transcoding info as playback has ended. This matches the behavior in FromStopEvent.
                    entry.TranscodeBitrate = null;
                    entry.HardwareAcceleration = null;
                    entry.TranscodedVideoCodec = null;
                    entry.TranscodedVideoContainer = null;
                    entry.TranscodedAudioCodec = null;
                    entry.TranscodeReasonsJson = null;
                    entry.TranscodeCompletionPercentage = null;

                    await _repository.Upsert(entry, cancellationToken).ConfigureAwait(false);
                    completedCount++;
                }
                else
                {
                    // Delete entry that doesn't meet minimum threshold
                    await _repository.DeleteByIdAsync(entry.Id, cancellationToken).ConfigureAwait(false);
                    deletedCount++;
                }
            }

            if (completedCount > 0 || deletedCount > 0)
            {
                _logger.LogInformation("Marked {CompletedCount} incomplete playback entries as completed and deleted {DeletedCount} entries that didn't meet the minimum threshold on startup", completedCount, deletedCount);
            }
            else
            {
                _logger.LogDebug("No incomplete playback entries found during startup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Jellydash startup cleanup");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
