using Npgsql;

namespace Bookstore.ScaleLab.Partitioning;

public static class PartitionCommands
{
    public static async Task<int> MaintainAsync(LabOptions options)
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(Db.Env("BOOKSTORE_PG"));
        PartitionMaintenanceJob job = new(dataSource, TimeProvider.System);
        await job.RunAsync(monthsAhead: options.GetInt("months-ahead", 3), retentionMonths: options.GetInt("retention-months", 36), CancellationToken.None);
        return 0;
    }
}
