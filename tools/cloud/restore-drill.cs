// Restore drill for a SQL Server you run yourself (VM or container): back up BookstoreDb, restore it under
// a new name, compare it with the source and report how long it took. Azure SQL: infra/cli/11-restore-drill.sh.
// Run: dotnet run tools/cloud/restore-drill.cs   (BOOKSTORE_DB = connection string to the source database)
#:package Microsoft.Data.SqlClient
using System.Diagnostics;
using Microsoft.Data.SqlClient;

SqlConnectionStringBuilder source = new(Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? throw new InvalidOperationException("Set BOOKSTORE_DB to the source connection string."));
string database = source.InitialCatalog;
string drill = $"{database}_RestoreDrill";
string backupFile = $"/var/opt/mssql/data/{database}_drill.bak"; // a path on the SQL Server machine

SqlConnectionStringBuilder master = new(source.ConnectionString) { InitialCatalog = "master" };
await using SqlConnection server = new(master.ConnectionString);
await server.OpenAsync();

Stopwatch backup = Stopwatch.StartNew();
await ExecAsync($"BACKUP DATABASE [{database}] TO DISK = N'{backupFile}' WITH INIT, COPY_ONLY, CHECKSUM");
backup.Stop();

// The restored copy needs its own file names next to the original ones
List<string> moves = [];
await using (SqlCommand files = new($"RESTORE FILELISTONLY FROM DISK = N'{backupFile}'", server))
await using (SqlDataReader reader = await files.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        string logical = reader.GetString(0);
        string physical = reader.GetString(1);
        string target = Path.Combine(Path.GetDirectoryName(physical)!, $"{drill}_{logical}{Path.GetExtension(physical)}");
        moves.Add($"MOVE N'{logical}' TO N'{target}'");
    }
}

Stopwatch restore = Stopwatch.StartNew();
await ExecAsync($"RESTORE DATABASE [{drill}] FROM DISK = N'{backupFile}' WITH {string.Join(", ", moves)}, REPLACE, CHECKSUM");
restore.Stop();

const string CountsSql = """
    SELECT s.name + '.' + t.name, SUM(p.rows)
    FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
    GROUP BY s.name, t.name ORDER BY 1
    """;
Dictionary<string, long> expected = await CountsAsync(database);
Dictionary<string, long> actual = await CountsAsync(drill);
int mismatches = expected.Count(e => !actual.TryGetValue(e.Key, out long rows) || rows != e.Value);

Console.WriteLine($"Backup  {database,-24} {backup.Elapsed.TotalSeconds,6:0.00} s");
Console.WriteLine($"Restore {drill,-24} {restore.Elapsed.TotalSeconds,6:0.00} s");
Console.WriteLine($"Tables compared: {expected.Count}, rows: {expected.Values.Sum()}, mismatches: {mismatches}");
Console.WriteLine(mismatches == 0 ? "Drill passed: the backup restores and matches the source." : "Drill FAILED.");

await ExecAsync($"ALTER DATABASE [{drill}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{drill}]");
return mismatches == 0 ? 0 : 1;

async Task ExecAsync(string sql)
{
    await using SqlCommand command = new(sql, server) { CommandTimeout = 600 };
    await command.ExecuteNonQueryAsync();
}

async Task<Dictionary<string, long>> CountsAsync(string db)
{
    await server.ChangeDatabaseAsync(db);
    Dictionary<string, long> counts = [];
    await using (SqlCommand command = new(CountsSql, server))
    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            counts[reader.GetString(0)] = reader.GetInt64(1);
        }
    }
    await server.ChangeDatabaseAsync("master");
    return counts;
}
