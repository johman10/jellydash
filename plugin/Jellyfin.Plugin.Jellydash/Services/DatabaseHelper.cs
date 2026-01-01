using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.Jellydash.Services;

/// <summary>
/// Handles schema migrations for the Jellydash database.
/// </summary>
public class DatabaseHelper
{
    private readonly string _databasePath;
    private readonly string _dataPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHelper"/> class.
    /// </summary>
    /// <param name="dataPath">The plugin datapath where the database file will be stored.</param>
    public DatabaseHelper(string dataPath)
    {
        var pluginDir = Path.Combine(dataPath, "plugins", "Jellydash");
        Directory.CreateDirectory(pluginDir);
        _dataPath = dataPath;
        _databasePath = Path.Combine(pluginDir, "jellydash.db");
    }

    /// <summary>
    /// Gets the folder path that contains SQL migration scripts.
    /// </summary>
    private static string MigrationFolderPath
    {
        get
        {
            var assemblyLocation = typeof(DatabaseHelper).Assembly.Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation)
                ?? throw new InvalidOperationException("Unable to determine plugin assembly directory.");

            return Path.Combine(assemblyDir, "Migrations");
        }
    }

    /// <summary>
    /// Gets connection string to be used for access to the DB.
    /// </summary>
    public string ConnectionString
    {
        get => $"Data Source={_databasePath};Mode=ReadWriteCreate;Cache=Shared";
    }

    /// <summary>
    /// Applies any pending migrations to the specified Jellydash database.
    /// </summary>
    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        // Read current schema version.
        using var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA user_version;";
        var result = pragmaCmd.ExecuteScalar();
        var version = Convert.ToInt32(result, CultureInfo.InvariantCulture);

        var currentVersion = version;

        var migrationScripts = LoadMigrationScripts();
        foreach (var (migrationVersion, scriptPath) in migrationScripts)
        {
            if (migrationVersion <= currentVersion)
            {
                continue;
            }

            ApplyMigrationFromFile(connection, migrationVersion, scriptPath);
            currentVersion = migrationVersion;
        }
    }

    private static IReadOnlyList<(int Version, string Path)> LoadMigrationScripts()
    {
        if (!Directory.Exists(MigrationFolderPath))
        {
            return Array.Empty<(int, string)>();
        }

        var files = Directory.GetFiles(MigrationFolderPath, "*.sql", SearchOption.TopDirectoryOnly);
        var list = new List<(int Version, string Path)>();

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file); // e.g. 001_Initial
            var underscoreIndex = name.IndexOf('_', StringComparison.Ordinal);
            var versionPart = underscoreIndex >= 0 ? name[..underscoreIndex] : name;

            if (int.TryParse(versionPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
            {
                list.Add((version, file));
            }
        }

        list.Sort((a, b) => a.Version.CompareTo(b.Version));

        return list;
    }

    private static void ApplyMigrationFromFile(SqliteConnection connection, int targetVersion, string scriptPath)
    {
        var sql = File.ReadAllText(scriptPath);

        var transaction = connection.BeginTransaction();

        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA user_version = " + targetVersion.ToString(CultureInfo.InvariantCulture) + ";";
        cmd.ExecuteNonQuery();
#pragma warning restore CA2100

        transaction.Commit();
    }
}
