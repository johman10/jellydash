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
/// Initializes a new instance of the <see cref="PlaybackEntryRepository"/> class.
/// </remarks>
/// <param name="databaseHelper">An instance of DatabaseHelper.</param>
public sealed class PlaybackEntryRepository(DatabaseHelper databaseHelper)
{
    private static readonly SemaphoreSlim DbLock = new(1, 1);
    private readonly DatabaseHelper _databaseHelper = databaseHelper;

    /// <summary>
    /// Retrieves a page of playback entries ordered from most recent to oldest.
    /// </summary>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="beforeId">Optional id of the last entry from the previous page.</param>
    /// <param name="beforeEndUtc">Optional end time of the last entry from the previous page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the entries and paging state.</returns>
    public async Task<(IReadOnlyList<PlaybackEntry> Entries, long? LastId, DateTime? LastEndUtc)> GetPageAsync(
        int limit,
        long? beforeId,
        DateTime? beforeEndUtc,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var results = new List<PlaybackEntry>(limit);
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
    ItemId,
    ContentKind,
    DisplayTitle,
    PrimaryImageUrl,
    PrimaryGenre,
    Year,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    UserId,
    UserName,
    UserImageUrl,
    ClientName,
    DeviceName,
    DeviceId,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsCompleted,
    IsPaused,
    VideoCodec,
    VideoContainer,
    VideoRange,
    VideoBitrate,
    VideoBitDepth,
    VideoHeight,
    VideoWidth,
    AudioLanguage,
    AudioCodec,
    AudioLayout,
    AudioBitrate,
    AudioSampleRate,
    SubtitleIsForced,
    SubtitleIsHearingImpaired,
    SubtitleCodec,
    SubtitleLanguage,
    IsVideoDirect,
    IsAudioDirect,
    TranscodeBitrate,
    HardwareAcceleration,
    TranscodedVideoCodec,
    TranscodedAudioCodec,
    TranscodeReasonsJson
FROM PlaybackEntries
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
    ItemId,
    ContentKind,
    DisplayTitle,
    PrimaryImageUrl,
    PrimaryGenre,
    Year,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    UserId,
    UserName,
    UserImageUrl,
    ClientName,
    DeviceName,
    DeviceId,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsCompleted,
    IsPaused,
    VideoCodec,
    VideoContainer,
    VideoRange,
    VideoBitrate,
    VideoBitDepth,
    VideoHeight,
    VideoWidth,
    AudioLanguage,
    AudioCodec,
    AudioLayout,
    AudioBitrate,
    AudioSampleRate,
    SubtitleIsForced,
    SubtitleIsHearingImpaired,
    SubtitleCodec,
    SubtitleLanguage,
    IsVideoDirect,
    IsAudioDirect,
    TranscodeBitrate,
    HardwareAcceleration,
    TranscodedVideoCodec,
    TranscodedAudioCodec,
    TranscodeReasonsJson
FROM PlaybackEntries
ORDER BY EndUtc DESC, Id DESC
LIMIT $limit;";
            }

            AddParameter(cmd, "$limit", limit);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entry = MapPlaybackEntry(reader, out var id, out var endUtc);
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
    /// Appends a playback entry to the store.
    /// </summary>
    /// <param name="entry">The entry to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AppendAsync(PlaybackEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO PlaybackEntries (
    ItemId,
    ContentKind,
    DisplayTitle,
    PrimaryImageUrl,
    PrimaryGenre,
    Year,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    UserId,
    UserName,
    UserImageUrl,
    ClientName,
    DeviceName,
    DeviceId,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsCompleted,
    IsPaused,
    VideoCodec,
    VideoContainer,
    VideoRange,
    VideoBitrate,
    VideoBitDepth,
    VideoHeight,
    VideoWidth,
    AudioLanguage,
    AudioCodec,
    AudioLayout,
    AudioBitrate,
    AudioSampleRate,
    SubtitleIsForced,
    SubtitleIsHearingImpaired,
    SubtitleCodec,
    SubtitleLanguage,
    IsVideoDirect,
    IsAudioDirect,
    TranscodeBitrate,
    HardwareAcceleration,
    TranscodedVideoCodec,
    TranscodedAudioCodec,
    TranscodeReasonsJson
) VALUES (
    $itemId,
    $contentKind,
    $displayTitle,
    $primaryImageUrl,
    $primaryGenre,
    $year,
    $seriesName,
    $seasonNumber,
    $episodeNumber,
    $userId,
    $userName,
    $userImageUrl,
    $clientName,
    $deviceName,
    $deviceId,
    $startUtc,
    $endUtc,
    $runtimeTicks,
    $startPositionTicks,
    $endPositionTicks,
    $startPercentage,
    $endPercentage,
    $isCompleted,
    $isPaused,
    $videoCodec,
    $videoContainer,
    $videoRange,
    $videoBitrate,
    $videoBitDepth,
    $videoHeight,
    $videoWidth,
    $audioLanguage,
    $audioCodec,
    $audioLayout,
    $audioBitrate,
    $audioSampleRate,
    $subtitleIsForced,
    $subtitleIsHearingImpaired,
    $subtitleCodec,
    $subtitleLanguage,
    $isVideoDirect,
    $isAudioDirect,
    $transcodeBitrate,
    $hardwareAcceleration,
    $transcodedVideoCodec,
    $transcodedAudioCodec,
    $transcodeReasonsJson
);";

            AddParameter(cmd, "$itemId", entry.ItemId.ToString("N", CultureInfo.InvariantCulture));
            AddParameter(cmd, "$contentKind", (int)entry.ContentKind);
            AddParameter(cmd, "$displayTitle", entry.DisplayTitle);
            AddParameter(cmd, "$primaryImageUrl", entry.PrimaryImageUrl);
            AddParameter(cmd, "$primaryGenre", entry.PrimaryGenre);
            AddParameter(cmd, "$year", entry.Year);
            AddParameter(cmd, "$seriesName", entry.SeriesName);
            AddParameter(cmd, "$seasonNumber", entry.SeasonNumber);
            AddParameter(cmd, "$episodeNumber", entry.EpisodeNumber);
            AddParameter(cmd, "$userId", entry.UserId.ToString("N", CultureInfo.InvariantCulture));
            AddParameter(cmd, "$userName", entry.UserName);
            AddParameter(cmd, "$userImageUrl", entry.UserImageUrl);
            AddParameter(cmd, "$clientName", entry.ClientName);
            AddParameter(cmd, "$deviceName", entry.DeviceName);
            AddParameter(cmd, "$deviceId", entry.DeviceId);
            AddParameter(cmd, "$startUtc", entry.StartUtc.ToString("O", CultureInfo.InvariantCulture));
            AddParameter(cmd, "$endUtc", entry.EndUtc.HasValue ? entry.EndUtc.Value.ToString("O", CultureInfo.InvariantCulture) : null);
            AddParameter(cmd, "$runtimeTicks", entry.RuntimeTicks);
            AddParameter(cmd, "$startPositionTicks", entry.StartPositionTicks);
            AddParameter(cmd, "$endPositionTicks", entry.EndPositionTicks);
            AddParameter(cmd, "$startPercentage", entry.StartPercentage);
            AddParameter(cmd, "$endPercentage", entry.EndPercentage);
            AddParameter(cmd, "$isCompleted", entry.IsCompleted ? 1 : 0);
            AddParameter(cmd, "$isPaused", entry.IsPaused ? 1 : 0);
            AddParameter(cmd, "$videoCodec", entry.VideoCodec);
            AddParameter(cmd, "$videoContainer", entry.VideoContainer);
            AddParameter(cmd, "$videoRange", entry.VideoRange);
            AddParameter(cmd, "$videoBitrate", entry.VideoBitrate);
            AddParameter(cmd, "$videoBitDepth", entry.VideoBitDepth);
            AddParameter(cmd, "$videoHeight", entry.VideoHeight);
            AddParameter(cmd, "$videoWidth", entry.VideoWidth);
            AddParameter(cmd, "$audioLanguage", entry.AudioLanguage);
            AddParameter(cmd, "$audioCodec", entry.AudioCodec);
            AddParameter(cmd, "$audioLayout", entry.AudioLayout);
            AddParameter(cmd, "$audioBitrate", entry.AudioBitrate);
            AddParameter(cmd, "$audioSampleRate", entry.AudioSampleRate);
            AddParameter(cmd, "$subtitleIsForced", entry.SubtitleIsForced.HasValue ? (entry.SubtitleIsForced.Value ? 1 : 0) : null);
            AddParameter(cmd, "$subtitleIsHearingImpaired", entry.SubtitleIsHearingImpaired.HasValue ? (entry.SubtitleIsHearingImpaired.Value ? 1 : 0) : null);
            AddParameter(cmd, "$subtitleCodec", entry.SubtitleCodec);
            AddParameter(cmd, "$subtitleLanguage", entry.SubtitleLanguage);
            AddParameter(cmd, "$isVideoDirect", entry.IsVideoDirect ? 1 : 0);
            AddParameter(cmd, "$isAudioDirect", entry.IsAudioDirect ? 1 : 0);
            AddParameter(cmd, "$transcodeBitrate", entry.TranscodeBitrate);
            AddParameter(cmd, "$hardwareAcceleration", entry.HardwareAcceleration);
            AddParameter(cmd, "$transcodedVideoCodec", entry.TranscodedVideoCodec);
            AddParameter(cmd, "$transcodedAudioCodec", entry.TranscodedAudioCodec);
            AddParameter(cmd, "$transcodeReasonsJson", entry.TranscodeReasonsJson);

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
    public async Task<IReadOnlyList<PlaybackEntry>> GetRecentAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var results = new List<PlaybackEntry>();

            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT
    Id,
    ItemId,
    ContentKind,
    DisplayTitle,
    PrimaryImageUrl,
    PrimaryGenre,
    Year,
    SeriesName,
    SeasonNumber,
    EpisodeNumber,
    UserId,
    UserName,
    UserImageUrl,
    ClientName,
    DeviceName,
    DeviceId,
    StartUtc,
    EndUtc,
    RuntimeTicks,
    StartPositionTicks,
    EndPositionTicks,
    StartPercentage,
    EndPercentage,
    IsCompleted,
    IsPaused,
    VideoCodec,
    VideoContainer,
    VideoRange,
    VideoBitrate,
    VideoBitDepth,
    VideoHeight,
    VideoWidth,
    AudioLanguage,
    AudioCodec,
    AudioLayout,
    AudioBitrate,
    AudioSampleRate,
    SubtitleIsForced,
    SubtitleIsHearingImpaired,
    SubtitleCodec,
    SubtitleLanguage,
    IsVideoDirect,
    IsAudioDirect,
    TranscodeBitrate,
    HardwareAcceleration,
    TranscodedVideoCodec,
    TranscodedAudioCodec,
    TranscodeReasonsJson
FROM PlaybackEntries
WHERE EndUtc >= $cutoffUtc
ORDER BY EndUtc DESC;";

            AddParameter(cmd, "$cutoffUtc", cutoffUtc.ToString("O", CultureInfo.InvariantCulture));

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entry = MapPlaybackEntry(reader, out _, out _);
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
            cmd.CommandText = "DELETE FROM PlaybackEntries WHERE EndUtc < $cutoffUtc;";
            AddParameter(cmd, "$cutoffUtc", cutoffUtc.ToString("O", CultureInfo.InvariantCulture));

            var removed = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return removed;
        }
        finally
        {
            DbLock.Release();
        }
    }

    private static PlaybackEntry MapPlaybackEntry(SqliteDataReader reader, out long id, out DateTime? endUtc)
    {
        id = reader.GetInt64(0);

        var itemId = Guid.Parse(reader.GetString(1));
        var contentKind = (ContentKind)reader.GetInt32(2);
        var displayTitle = reader.GetString(3);

        var isPrimaryImageUrlNull = reader.IsDBNull(4);
        var isPrimaryGenreNull = reader.IsDBNull(5);
        var isYearNull = reader.IsDBNull(6);
        var isSeriesNameNull = reader.IsDBNull(7);
        var isSeasonNumberNull = reader.IsDBNull(8);
        var isEpisodeNumberNull = reader.IsDBNull(9);

        var userId = Guid.Parse(reader.GetString(10));
        var userName = reader.GetString(11);
        var isUserImageUrlNull = reader.IsDBNull(12);

        var clientName = reader.GetString(13);
        var deviceName = reader.GetString(14);
        var isDeviceIdNull = reader.IsDBNull(15);

        var startUtc = ParseUtc(reader.GetString(16));
        DateTime? parsedEndUtc = null;
        if (!reader.IsDBNull(17))
        {
            parsedEndUtc = ParseUtc(reader.GetString(17));
        }

        endUtc = parsedEndUtc;

        var isRuntimeTicksNull = reader.IsDBNull(18);
        var isStartPositionTicksNull = reader.IsDBNull(19);
        var isEndPositionTicksNull = reader.IsDBNull(20);
        var isStartPercentageNull = reader.IsDBNull(21);
        var isEndPercentageNull = reader.IsDBNull(22);

        var isCompleted = reader.GetInt32(23) != 0;
        var isPaused = reader.GetInt32(24) != 0;

        var isVideoCodecNull = reader.IsDBNull(25);
        var isVideoContainerNull = reader.IsDBNull(26);
        var isVideoRangeNull = reader.IsDBNull(27);
        var isVideoBitrateNull = reader.IsDBNull(28);
        var isVideoBitDepthNull = reader.IsDBNull(29);
        var isVideoHeightNull = reader.IsDBNull(30);
        var isVideoWidthNull = reader.IsDBNull(31);

        var isAudioLanguageNull = reader.IsDBNull(32);
        var isAudioCodecNull = reader.IsDBNull(33);
        var isAudioLayoutNull = reader.IsDBNull(34);
        var isAudioBitrateNull = reader.IsDBNull(35);
        var isAudioSampleRateNull = reader.IsDBNull(36);

        var isSubtitleIsForcedNull = reader.IsDBNull(37);
        var isSubtitleIsHearingImpairedNull = reader.IsDBNull(38);
        var isSubtitleCodecNull = reader.IsDBNull(39);
        var isSubtitleLanguageNull = reader.IsDBNull(40);

        var isVideoDirect = reader.GetInt32(41) != 0;
        var isAudioDirect = reader.GetInt32(42) != 0;

        var isTranscodeBitrateNull = reader.IsDBNull(43);
        var isHardwareAccelerationNull = reader.IsDBNull(44);
        var isTranscodedVideoCodecNull = reader.IsDBNull(45);
        var isTranscodedAudioCodecNull = reader.IsDBNull(46);
        var isTranscodeReasonsJsonNull = reader.IsDBNull(47);

        var entry = new PlaybackEntry
        {
            ItemId = itemId,
            ContentKind = contentKind,
            DisplayTitle = displayTitle,
            PrimaryImageUrl = isPrimaryImageUrlNull ? null : reader.GetString(4),
            PrimaryGenre = isPrimaryGenreNull ? null : reader.GetString(5),
            Year = isYearNull ? null : reader.GetInt32(6),
            SeriesName = isSeriesNameNull ? null : reader.GetString(7),
            SeasonNumber = isSeasonNumberNull ? null : reader.GetInt32(8),
            EpisodeNumber = isEpisodeNumberNull ? null : reader.GetInt32(9),
            UserId = userId,
            UserName = userName,
            UserImageUrl = isUserImageUrlNull ? null : reader.GetString(12),
            ClientName = clientName,
            DeviceName = deviceName,
            DeviceId = isDeviceIdNull ? null : reader.GetString(15),
            StartUtc = startUtc,
            EndUtc = parsedEndUtc,
            RuntimeTicks = isRuntimeTicksNull ? null : reader.GetInt64(18),
            StartPositionTicks = isStartPositionTicksNull ? null : reader.GetInt64(19),
            EndPositionTicks = isEndPositionTicksNull ? null : reader.GetInt64(20),
            StartPercentage = isStartPercentageNull ? null : reader.GetDouble(21),
            EndPercentage = isEndPercentageNull ? null : reader.GetDouble(22),
            IsCompleted = isCompleted,
            IsPaused = isPaused,
            VideoCodec = isVideoCodecNull ? null : reader.GetString(25),
            VideoContainer = isVideoContainerNull ? null : reader.GetString(26),
            VideoRange = isVideoRangeNull ? null : reader.GetString(27),
            VideoBitrate = isVideoBitrateNull ? null : reader.GetInt32(28),
            VideoBitDepth = isVideoBitDepthNull ? null : reader.GetInt32(29),
            VideoHeight = isVideoHeightNull ? null : reader.GetInt32(30),
            VideoWidth = isVideoWidthNull ? null : reader.GetInt32(31),
            AudioLanguage = isAudioLanguageNull ? null : reader.GetString(32),
            AudioCodec = isAudioCodecNull ? null : reader.GetString(33),
            AudioLayout = isAudioLayoutNull ? null : reader.GetString(34),
            AudioBitrate = isAudioBitrateNull ? null : reader.GetInt32(35),
            AudioSampleRate = isAudioSampleRateNull ? null : reader.GetInt32(36),
            SubtitleIsForced = isSubtitleIsForcedNull ? null : reader.GetInt32(37) != 0,
            SubtitleIsHearingImpaired = isSubtitleIsHearingImpairedNull ? null : reader.GetInt32(38) != 0,
            SubtitleCodec = isSubtitleCodecNull ? null : reader.GetString(39),
            SubtitleLanguage = isSubtitleLanguageNull ? null : reader.GetString(40),
            IsVideoDirect = isVideoDirect,
            IsAudioDirect = isAudioDirect,
            TranscodeBitrate = isTranscodeBitrateNull ? null : reader.GetInt32(43),
            HardwareAcceleration = isHardwareAccelerationNull ? null : reader.GetString(44),
            TranscodedVideoCodec = isTranscodedVideoCodecNull ? null : reader.GetString(45),
            TranscodedAudioCodec = isTranscodedAudioCodecNull ? null : reader.GetString(46),
            TranscodeReasonsJson = isTranscodeReasonsJsonNull ? null : reader.GetString(47)
        };

        return entry;
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
