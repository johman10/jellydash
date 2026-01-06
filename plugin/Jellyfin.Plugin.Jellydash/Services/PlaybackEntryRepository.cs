using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Jellyfin.Plugin.Jellydash.Constants;
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the entries and paging state.</returns>
    public async Task<IReadOnlyList<PlaybackEntry>> GetNowPlayingAsync(
          CancellationToken cancellationToken)
    {
        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            // Create a query that retrieves all playback entries, ordered by EndUtc DESC, Id DESC for paging
            var sql = $@"
                    SELECT *
                    FROM PlaybackEntries
                    WHERE IsCompleted = 0
                    ORDER BY StartUtc DESC, Id DESC;";
            return (await connection.QueryAsync<PlaybackEntry>(sql).ConfigureAwait(false)).AsList();
        }
        finally
        {
            DbLock.Release();
        }
    }

    /// <summary>
    /// Retrieves a page of playback entries ordered from most recent to oldest.
    /// </summary>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="beforeId">Optional id of the last entry from the previous page.</param>
    /// <param name="beforeEndUtc">Optional end time of the last entry from the previous page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the entries and paging state.</returns>
    public async Task<(IReadOnlyList<PlaybackEntry> Entries, int? LastId, DateTime? LastEndUtc)> GetHistoryPageAsync(
          int limit,
          int? beforeId,
          DateTime? beforeEndUtc,
          CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<PlaybackEntry> entries;
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            // Create a query that retrieves all playback entries, ordered by EndUtc DESC, Id DESC for paging
            if (beforeId.HasValue && beforeEndUtc.HasValue)
            {
                var sql = @"
                        SELECT *
                        FROM PlaybackEntries
                        WHERE (EndUtc < @BeforeEndUtc)
                           OR (EndUtc = @BeforeEndUtc AND Id < @BeforeId)
                        ORDER BY EndUtc DESC, Id DESC
                        LIMIT @Limit;";
                entries = (await connection.QueryAsync<PlaybackEntry>(sql, new { BeforeEndUtc = beforeEndUtc.Value, BeforeId = beforeId.Value, Limit = limit }).ConfigureAwait(false)).AsList();
            }
            else
            {
                var sql = $@"
                        SELECT *
                        FROM PlaybackEntries
                        ORDER BY EndUtc DESC, Id DESC
                        LIMIT @Limit;";
                entries = (await connection.QueryAsync<PlaybackEntry>(sql, new { Limit = limit }).ConfigureAwait(false)).AsList();
            }

            int? lastId = null;
            DateTime? lastEndUtc = null;
            if (entries.Count > 0)
            {
                // Assuming PlaybackEntry has Id and EndUtc properties
                var lastEntry = entries[^1];
                lastId = lastEntry.Id;
                lastEndUtc = lastEntry.EndUtc;
            }

            return (entries, lastId, lastEndUtc);
        }
        finally
        {
            DbLock.Release();
        }
    }

    /// <summary>
    /// Gets an the latests incomplete playback entry by its PlaybackId, if present.
    /// </summary>
    /// <param name="playbackId">The Jellyfin playback identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="PlaybackEntry"/> or <c>null</c> if none exists.</returns>
    public async Task<PlaybackEntry?> GetRecentlyIncompletedByPlaybackIdAsync(Guid playbackId, CancellationToken cancellationToken)
    {
        if (playbackId == Guid.Empty)
        {
            throw new ArgumentException("PlaybackId must be a valid non-empty GUID.", nameof(playbackId));
        }

        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
SELECT *
FROM PlaybackEntries
WHERE PlaybackId = @PlaybackId AND IsCompleted = 0
ORDER BY StartUtc DESC
LIMIT 1;";

            var command = new CommandDefinition(sql, new { PlaybackId = playbackId }, cancellationToken: cancellationToken);
            var entry = await connection.QueryFirstOrDefaultAsync<PlaybackEntry>(command).ConfigureAwait(false);
            return entry;
        }
        finally
        {
            DbLock.Release();
        }
    }

    /// <summary>
    /// Gets an the latests completed playback entry by its PlaybackId, if present.
    /// </summary>
    /// <param name="playbackId">The Jellyfin playback identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="PlaybackEntry"/> or <c>null</c> if none exists.</returns>
    public async Task<PlaybackEntry?> GetRecentlyCompletedByPlaybackIdAsync(Guid playbackId, CancellationToken cancellationToken)
    {
        if (playbackId == Guid.Empty)
        {
            throw new ArgumentException("PlaybackId must be a valid non-empty GUID.", nameof(playbackId));
        }

        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var sql = $@"
SELECT *
FROM PlaybackEntries
WHERE PlaybackId = @PlaybackId AND IsCompleted = 1
ORDER BY StartUtc DESC
LIMIT 1;";

            var command = new CommandDefinition(sql, new { PlaybackId = playbackId }, cancellationToken: cancellationToken);
            var entry = await connection.QueryFirstOrDefaultAsync<PlaybackEntry>(command).ConfigureAwait(false);
            return entry;
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
            var query = $@"INSERT INTO PlaybackEntries ({PlaybackEntryConstants.Columns}) VALUES ({PlaybackEntryConstants.Parameters});";

            await connection.ExecuteAsync(query, entry).ConfigureAwait(false);
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
            var sql = "DELETE FROM PlaybackEntries WHERE EndUtc < @cutoffUtc;";
            return await connection.ExecuteAsync(sql, new { cutoffUtc }).ConfigureAwait(false);
        }
        finally
        {
            DbLock.Release();
        }
    }

    /// <summary>
    /// Inserts or updates a playback entry based on the passed entry.
    /// </summary>
    /// <param name="entry">The entry to insert or update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Upsert(PlaybackEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.PlaybackId == Guid.Empty)
        {
            throw new ArgumentException("PlaybackId must be a valid non-empty GUID.", nameof(entry));
        }

        await DbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_databaseHelper.ConnectionString);
            var updateSql = $"UPDATE PlaybackEntries SET {PlaybackEntryConstants.UpdateSetClause} WHERE PlaybackId = @PlaybackId AND IsCompleted = 0;";
            var updated = await connection.ExecuteAsync(updateSql, entry).ConfigureAwait(false);
            if (updated > 0)
            {
                return;
            }

            var insertSql = $"INSERT INTO PlaybackEntries ({PlaybackEntryConstants.Columns}) VALUES ({PlaybackEntryConstants.Parameters});";
            await connection.ExecuteAsync(insertSql, entry).ConfigureAwait(false);
        }
        finally
        {
            DbLock.Release();
        }
    }
}
