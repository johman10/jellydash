using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellydash.Models;

namespace Jellyfin.Plugin.Jellydash.Services;

/// <summary>
/// Simple file-backed repository for Jellydash history entries.
/// </summary>
internal sealed class HistoryRepository
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string HistoryFilePath
    {
        get
        {
            var plugin = Plugin.Instance;
            if (plugin is null)
            {
                throw new InvalidOperationException("Jellydash plugin instance is not initialized.");
            }

            var dataPath = plugin.ApplicationPaths.DataPath;
            var pluginDir = Path.Combine(dataPath, "plugins", "Jellydash");
            Directory.CreateDirectory(pluginDir);
            return Path.Combine(pluginDir, "history.jsonl");
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
        await FileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var stream = new FileStream(
                HistoryFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                4096,
                useAsync: true);

            await JsonSerializer.SerializeAsync(stream, entry, SerializeOptions, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { (byte)'\n' }), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            FileLock.Release();
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
        await FileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = HistoryFilePath;
            if (!File.Exists(path))
            {
                return Array.Empty<HistoryEntry>();
            }

            var results = new List<HistoryEntry>();

            await foreach (var line in ReadLinesAsync(path, cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var entry = JsonSerializer.Deserialize<HistoryEntry>(line, DeserializeOptions);
                    if (entry is null)
                    {
                        continue;
                    }

                    if (entry.EndUtc >= cutoffUtc)
                    {
                        results.Add(entry);
                    }
                }
                catch
                {
                    // Ignore malformed lines to avoid breaking history entirely.
                }
            }

            return results;
        }
        finally
        {
            FileLock.Release();
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
        await FileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = HistoryFilePath;
            if (!File.Exists(path))
            {
                return 0;
            }

            var kept = new List<HistoryEntry>();
            var removed = 0;

            await foreach (var line in ReadLinesAsync(path, cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                HistoryEntry? entry;
                try
                {
                    entry = JsonSerializer.Deserialize<HistoryEntry>(line, DeserializeOptions);
                }
                catch
                {
                    // Drop corrupt lines.
                    continue;
                }

                if (entry is null)
                {
                    continue;
                }

                if (entry.EndUtc < cutoffUtc)
                {
                    removed++;
                }
                else
                {
                    kept.Add(entry);
                }
            }

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true))
            {
                foreach (var entry in kept)
                {
                    await JsonSerializer.SerializeAsync(stream, entry, SerializeOptions, cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { (byte)'\n' }), cancellationToken).ConfigureAwait(false);
                }
            }

            return removed;
        }
        finally
        {
            FileLock.Release();
        }
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            yield return line;
        }
    }
}
