using Jellyfin.Plugin.Jellydash.Services;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.Jellydash.Tests;

[Collection("JellydashPluginTests")]
public class DatabaseHelperTests
{
    [Fact]
    public void Initialize_CreatesHistoryEntriesTable()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "JellydashPluginTests", "Initialize_CreatesHistoryEntriesTable");
        Directory.CreateDirectory(tempRoot);

        var helper = new DatabaseHelper(tempRoot);
        helper.Initialize();

        using var connection = new SqliteConnection(helper.ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='HistoryEntries';";
        var result = cmd.ExecuteScalar();

        Assert.Equal("HistoryEntries", result as string);
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "JellydashPluginTests", "Initialize_IsIdempotent");
        Directory.CreateDirectory(tempRoot);

        var helper = new DatabaseHelper(tempRoot);

        // Should not throw when called multiple times.
        helper.Initialize();
        helper.Initialize();
    }
}
