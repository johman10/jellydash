using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Models;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.Jellydash.Services;

/// <summary>
/// SQLite-backed repository for Jellydash history entries.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HistoryRepository"/> class.
/// </remarks>
/// <param name="databaseHelper">An instance of DatabaseHelper.</param>
public sealed class HistoryRepository(DatabaseHelper databaseHelper)
{
    private static readonly SemaphoreSlim DbLock = new(1, 1);
    private readonly DatabaseHelper _databaseHelper = databaseHelper;

    /// <summary>
    /// Retrieves a page of history entries ordered from most recent to oldest.
    /// </summary>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="beforeId">Optional id of the last entry from the previous page.</param>
    /// <param name="beforeEndUtc">Optional end time of the last entry from the previous page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the entries and paging state.</returns>
    public async Task<(IReadOnlyList<HistoryEntry> Entries, long? LastId, DateTime? LastEndUtc)> GetPageAsync(
          int limit,
          long? beforeId,
          DateTime? beforeEndUtc,
          CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var results = new List<HistoryEntry>(limit);
            long? lastId = null;
            DateTime? lastEndUtc = null;

            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();

            if (beforeId.HasValue && beforeEndUtc.HasValue)
            {
                cmd.CommandText = @"
SELECT
    Id,
    UserId,
    UserName,
    UserImageUrl,
    ItemId,
    ClientName,
    DeviceName,
    MediaType,
    ItemName,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    ProductionYear,
    PrimaryGenre,
    PrimaryImagePath,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsDownload,
    DownloadSizeBytes,
    DownloadedBytes,
    DownloadProgressPercent,
    Bitrate,
    IsVideoDirectStream,
    IsAudioDirectStream,
    IsTranscoding,
    TranscodeVideoCodec,
    TranscodeAudioCodec,
    TranscodeContainer,
    TranscodeHardwareAcceleration
FROM HistoryEntries
WHERE (EndUtc < $cursorEndUtc)
   OR (EndUtc = $cursorEndUtc AND Id < $cursorId)
ORDER BY EndUtc DESC, Id DESC
LIMIT $limit;";

                AddParameter(cmd, "$cursorEndUtc", beforeEndUtc.Value.ToString("O", CultureInfo.InvariantCulture));
                AddParameter(cmd, "$cursorId", beforeId.Value);
            }
            else
            {
                cmd.CommandText = @"
SELECT
    Id,
    UserId,
    UserName,
    UserImageUrl,
    ItemId,
    ClientName,
    DeviceName,
    MediaType,
    ItemName,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    ProductionYear,
    PrimaryGenre,
    PrimaryImagePath,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsDownload,
    DownloadSizeBytes,
    DownloadedBytes,
    DownloadProgressPercent,
    Bitrate,
    IsVideoDirectStream,
    IsAudioDirectStream,
    IsTranscoding,
    TranscodeVideoCodec,
    TranscodeAudioCodec,
    TranscodeContainer,
    TranscodeHardwareAcceleration
FROM HistoryEntries
ORDER BY EndUtc DESC, Id DESC
LIMIT $limit;";
            }

            AddParameter(cmd, "$limit", limit);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = reader.GetInt64(0);
                var endUtcRaw = reader.GetString(16);
                var endUtc = ParseUtc(endUtcRaw);

                var isUserImageNull = await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false);
                var isClientNameNull = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false);
                var isDeviceNameNull = await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false);
                var isSeriesNameNull = await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false);
                var isSeasonNumberNull = await reader.IsDBNullAsync(10, cancellationToken).ConfigureAwait(false);
                var isEpisodeNumberNull = await reader.IsDBNullAsync(11, cancellationToken).ConfigureAwait(false);
                var isProductionYearNull = await reader.IsDBNullAsync(12, cancellationToken).ConfigureAwait(false);
                var isPrimaryGenreNull = await reader.IsDBNullAsync(13, cancellationToken).ConfigureAwait(false);
                var isPrimaryImagePathNull = await reader.IsDBNullAsync(14, cancellationToken).ConfigureAwait(false);
                var isRuntimeTicksNull = await reader.IsDBNullAsync(17, cancellationToken).ConfigureAwait(false);
                var isStartPosNull = await reader.IsDBNullAsync(18, cancellationToken).ConfigureAwait(false);
                var isEndPosNull = await reader.IsDBNullAsync(19, cancellationToken).ConfigureAwait(false);
                var isDownloadSizeNull = await reader.IsDBNullAsync(23, cancellationToken).ConfigureAwait(false);
                var isDownloadedBytesNull = await reader.IsDBNullAsync(24, cancellationToken).ConfigureAwait(false);
                var isDownloadPctNull = await reader.IsDBNullAsync(25, cancellationToken).ConfigureAwait(false);
                var isBitrateNull = await reader.IsDBNullAsync(26, cancellationToken).ConfigureAwait(false);
                var isTranscodeVideoNull = await reader.IsDBNullAsync(30, cancellationToken).ConfigureAwait(false);
                var isTranscodeAudioNull = await reader.IsDBNullAsync(31, cancellationToken).ConfigureAwait(false);
                var isTranscodeContainerNull = await reader.IsDBNullAsync(32, cancellationToken).ConfigureAwait(false);
                var isTranscodeHwNull = await reader.IsDBNullAsync(33, cancellationToken).ConfigureAwait(false);

                var entry = new HistoryEntry
                {
                    UserId = Guid.Parse(reader.GetString(1)),
                    UserName = reader.GetString(2),
                    UserImageUrl = isUserImageNull ? null : reader.GetString(3),
                    ItemId = Guid.Parse(reader.GetString(4)),
                    ClientName = isClientNameNull ? null : reader.GetString(5),
                    DeviceName = isDeviceNameNull ? null : reader.GetString(6),
                    MediaType = reader.GetString(7),
                    ItemName = reader.GetString(8),
                    SeriesName = isSeriesNameNull ? null : reader.GetString(9),
                    SeasonNumber = isSeasonNumberNull ? null : reader.GetInt32(10),
                    EpisodeNumber = isEpisodeNumberNull ? null : reader.GetInt32(11),
                    ProductionYear = isProductionYearNull ? null : reader.GetInt32(12),
                    PrimaryGenre = isPrimaryGenreNull ? null : reader.GetString(13),
                    PrimaryImagePath = isPrimaryImagePathNull ? null : reader.GetString(14),
                    StartUtc = ParseUtc(reader.GetString(15)),
                    EndUtc = endUtc,
                    RuntimeTicks = isRuntimeTicksNull ? null : reader.GetInt64(17),
                    StartPositionTicks = isStartPosNull ? null : reader.GetInt64(18),
                    EndPositionTicks = isEndPosNull ? null : reader.GetInt64(19),
                    StartPercentage = reader.GetDouble(20),
                    EndPercentage = reader.GetDouble(21),
                    IsDownload = reader.GetInt32(22) != 0,
                    DownloadSizeBytes = isDownloadSizeNull ? null : reader.GetInt64(23),
                    DownloadedBytes = isDownloadedBytesNull ? null : reader.GetInt64(24),
                    DownloadProgressPercent = isDownloadPctNull ? null : reader.GetDouble(25),
                    Bitrate = isBitrateNull ? null : reader.GetInt64(26),
                    IsVideoDirectStream = reader.GetInt32(27) != 0,
                    IsAudioDirectStream = reader.GetInt32(28) != 0,
                    IsTranscoding = reader.GetInt32(29) != 0,
                    TranscodeVideoCodec = isTranscodeVideoNull ? string.Empty : reader.GetString(30),
                    TranscodeAudioCodec = isTranscodeAudioNull ? string.Empty : reader.GetString(31),
                    TranscodeContainer = isTranscodeContainerNull ? string.Empty : reader.GetString(32),
                    TranscodeHardwareAcceleration = isTranscodeHwNull ? string.Empty : reader.GetString(33)
                };

                results.Add(entry);
                lastId = id;
                lastEndUtc = endUtc;
            }

            return (results, lastId, lastEndUtc);
        }
        finally
        {
            DbLock.Release();
        }
    }

    /// <summary>
    /// Appends a history entry to the store.
    /// </summary>
    /// <param name="entry">The entry to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AppendAsync(HistoryEntry entry, CancellationToken cancellationToken)
    {
        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO HistoryEntries (
    UserId,
    UserName,
    UserImageUrl,
    ItemId,
    ClientName,
    DeviceName,
    MediaType,
    ItemName,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    ProductionYear,
    PrimaryGenre,
    PrimaryImagePath,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsDownload,
    DownloadSizeBytes,
    DownloadedBytes,
    DownloadProgressPercent,
    Bitrate,
    IsVideoDirectStream,
    IsAudioDirectStream,
    IsTranscoding,
    TranscodeVideoCodec,
    TranscodeAudioCodec,
    TranscodeContainer,
    TranscodeHardwareAcceleration
) VALUES (
    $userId,
    $userName,
    $userImageUrl,
    $itemId,
    $clientName,
    $deviceName,
    $mediaType,
    $itemName,
    $seriesName,
    $seasonNumber,
    $episodeNumber,
    $productionYear,
    $primaryGenre,
    $primaryImagePath,
    $startUtc,
    $endUtc,
    $runtimeTicks,
    $startPositionTicks,
    $endPositionTicks,
    $startPercentage,
    $endPercentage,
    $isDownload,
    $downloadSizeBytes,
    $downloadedBytes,
    $downloadProgressPercent,
    $bitrate,
    $isVideoDirectStream,
    $isAudioDirectStream,
    $isTranscoding,
    $transcodeVideoCodec,
    $transcodeAudioCodec,
    $transcodeContainer,
    $transcodeHardwareAcceleration
);";

            AddParameter(cmd, "$userId", entry.UserId.ToString("N", CultureInfo.InvariantCulture));
            AddParameter(cmd, "$userName", entry.UserName);
            AddParameter(cmd, "$userImageUrl", entry.UserImageUrl);
            AddParameter(cmd, "$itemId", entry.ItemId.ToString("N", CultureInfo.InvariantCulture));
            AddParameter(cmd, "$clientName", entry.ClientName);
            AddParameter(cmd, "$deviceName", entry.DeviceName);
            AddParameter(cmd, "$mediaType", entry.MediaType);
            AddParameter(cmd, "$itemName", entry.ItemName);
            AddParameter(cmd, "$seriesName", entry.SeriesName);
            AddParameter(cmd, "$seasonNumber", entry.SeasonNumber);
            AddParameter(cmd, "$episodeNumber", entry.EpisodeNumber);
            AddParameter(cmd, "$productionYear", entry.ProductionYear);
            AddParameter(cmd, "$primaryGenre", entry.PrimaryGenre);
            AddParameter(cmd, "$primaryImagePath", entry.PrimaryImagePath);
            AddParameter(cmd, "$startUtc", entry.StartUtc.ToString("O", CultureInfo.InvariantCulture));
            AddParameter(cmd, "$endUtc", entry.EndUtc.ToString("O", CultureInfo.InvariantCulture));
            AddParameter(cmd, "$runtimeTicks", entry.RuntimeTicks);
            AddParameter(cmd, "$startPositionTicks", entry.StartPositionTicks);
            AddParameter(cmd, "$endPositionTicks", entry.EndPositionTicks);
            AddParameter(cmd, "$startPercentage", entry.StartPercentage);
            AddParameter(cmd, "$endPercentage", entry.EndPercentage);
            AddParameter(cmd, "$isDownload", entry.IsDownload ? 1 : 0);
            AddParameter(cmd, "$downloadSizeBytes", entry.DownloadSizeBytes);
            AddParameter(cmd, "$downloadedBytes", entry.DownloadedBytes);
            AddParameter(cmd, "$downloadProgressPercent", entry.DownloadProgressPercent);
            AddParameter(cmd, "$bitrate", entry.Bitrate);
            AddParameter(cmd, "$isVideoDirectStream", entry.IsVideoDirectStream ? 1 : 0);
            AddParameter(cmd, "$isAudioDirectStream", entry.IsAudioDirectStream ? 1 : 0);
            AddParameter(cmd, "$isTranscoding", entry.IsTranscoding ? 1 : 0);
            AddParameter(cmd, "$transcodeVideoCodec", entry.TranscodeVideoCodec);
            AddParameter(cmd, "$transcodeAudioCodec", entry.TranscodeAudioCodec);
            AddParameter(cmd, "$transcodeContainer", entry.TranscodeContainer);
            AddParameter(cmd, "$transcodeHardwareAcceleration", entry.TranscodeHardwareAcceleration);

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            DbLock.Release();
        }
    }

    /// <summary>
    /// Reads all entries with an end time greater than or equal to the specified cutoff.
    /// </summary>
    /// <param name="cutoffUtc">UTC cutoff time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recent history entries.</returns>
    public async Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var results = new List<HistoryEntry>();

            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT
    UserId,
    UserName,
    UserImageUrl,
    ItemId,
    ClientName,
    DeviceName,
    MediaType,
    ItemName,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    ProductionYear,
    PrimaryGenre,
    PrimaryImagePath,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsDownload,
    DownloadSizeBytes,
    DownloadedBytes,
    DownloadProgressPercent,
    Bitrate,
    IsVideoDirectStream,
    IsAudioDirectStream,
    IsTranscoding,
    TranscodeVideoCodec,
    TranscodeAudioCodec,
    TranscodeContainer,
    TranscodeHardwareAcceleration
FROM HistoryEntries
WHERE EndUtc >= $cutoffUtc
ORDER BY EndUtc DESC;";

            AddParameter(cmd, "$cutoffUtc", cutoffUtc.ToString("O", CultureInfo.InvariantCulture));

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var isUserImageNull = await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false);
                var isClientNameNull = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false);
                var isDeviceNameNull = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false);
                var isSeriesNameNull = await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false);
                var isSeasonNumberNull = await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false);
                var isEpisodeNumberNull = await reader.IsDBNullAsync(10, cancellationToken).ConfigureAwait(false);
                var isProductionYearNull = await reader.IsDBNullAsync(11, cancellationToken).ConfigureAwait(false);
                var isPrimaryGenreNull = await reader.IsDBNullAsync(12, cancellationToken).ConfigureAwait(false);
                var isPrimaryImagePathNull = await reader.IsDBNullAsync(13, cancellationToken).ConfigureAwait(false);
                var isRuntimeTicksNull = await reader.IsDBNullAsync(16, cancellationToken).ConfigureAwait(false);
                var isStartPosNull = await reader.IsDBNullAsync(17, cancellationToken).ConfigureAwait(false);
                var isEndPosNull = await reader.IsDBNullAsync(18, cancellationToken).ConfigureAwait(false);
                var isDownloadSizeNull = await reader.IsDBNullAsync(22, cancellationToken).ConfigureAwait(false);
                var isDownloadedBytesNull = await reader.IsDBNullAsync(23, cancellationToken).ConfigureAwait(false);
                var isDownloadPctNull = await reader.IsDBNullAsync(24, cancellationToken).ConfigureAwait(false);
                var isBitrateNull = await reader.IsDBNullAsync(25, cancellationToken).ConfigureAwait(false);
                var isTranscodeVideoNull = await reader.IsDBNullAsync(29, cancellationToken).ConfigureAwait(false);
                var isTranscodeAudioNull = await reader.IsDBNullAsync(30, cancellationToken).ConfigureAwait(false);
                var isTranscodeContainerNull = await reader.IsDBNullAsync(31, cancellationToken).ConfigureAwait(false);
                var isTranscodeHwNull = await reader.IsDBNullAsync(32, cancellationToken).ConfigureAwait(false);

                var entry = new HistoryEntry
                {
                    UserId = Guid.Parse(reader.GetString(0)),
                    UserName = reader.GetString(1),
                    UserImageUrl = isUserImageNull ? null : reader.GetString(2),
                    ItemId = Guid.Parse(reader.GetString(3)),
                    ClientName = isClientNameNull ? null : reader.GetString(4),
                    DeviceName = isDeviceNameNull ? null : reader.GetString(5),
                    MediaType = reader.GetString(6),
                    ItemName = reader.GetString(7),
                    SeriesName = isSeriesNameNull ? null : reader.GetString(8),
                    SeasonNumber = isSeasonNumberNull ? null : reader.GetInt32(9),
                    EpisodeNumber = isEpisodeNumberNull ? null : reader.GetInt32(10),
                    ProductionYear = isProductionYearNull ? null : reader.GetInt32(11),
                    PrimaryGenre = isPrimaryGenreNull ? null : reader.GetString(12),
                    PrimaryImagePath = isPrimaryImagePathNull ? null : reader.GetString(13),
                    StartUtc = ParseUtc(reader.GetString(14)),
                    EndUtc = ParseUtc(reader.GetString(15)),
                    RuntimeTicks = isRuntimeTicksNull ? null : reader.GetInt64(16),
                    StartPositionTicks = isStartPosNull ? null : reader.GetInt64(17),
                    EndPositionTicks = isEndPosNull ? null : reader.GetInt64(18),
                    StartPercentage = reader.GetDouble(19),
                    EndPercentage = reader.GetDouble(20),
                    IsDownload = reader.GetInt32(21) != 0,
                    DownloadSizeBytes = isDownloadSizeNull ? null : reader.GetInt64(22),
                    DownloadedBytes = isDownloadedBytesNull ? null : reader.GetInt64(23),
                    DownloadProgressPercent = isDownloadPctNull ? null : reader.GetDouble(24),
                    Bitrate = isBitrateNull ? null : reader.GetInt64(25),
                    IsVideoDirectStream = reader.GetInt32(26) != 0,
                    IsAudioDirectStream = reader.GetInt32(27) != 0,
                    IsTranscoding = reader.GetInt32(28) != 0,
                    TranscodeVideoCodec = isTranscodeVideoNull ? string.Empty : reader.GetString(29),
                    TranscodeAudioCodec = isTranscodeAudioNull ? string.Empty : reader.GetString(30),
                    TranscodeContainer = isTranscodeContainerNull ? string.Empty : reader.GetString(31),
                    TranscodeHardwareAcceleration = isTranscodeHwNull ? string.Empty : reader.GetString(32)
                };

                results.Add(entry);
            }

            return results;
        }
        finally
        {
            DbLock.Release();
        }
    }

    /// <summary>
    /// Deletes entries older than the specified cutoff.
    /// </summary>
    /// <param name="cutoffUtc">UTC cutoff time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of removed entries.</returns>
    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM HistoryEntries WHERE EndUtc < $cutoffUtc;";
            AddParameter(cmd, "$cutoffUtc", cutoffUtc.ToString("O", CultureInfo.InvariantCulture));

            var removed = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return removed;
        }
        finally
        {
            DbLock.Release();
        }
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static DateTime ParseUtc(string value)
    {
        var dt = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }
}
