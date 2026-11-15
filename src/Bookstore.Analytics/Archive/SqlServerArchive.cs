using System.Data;
using Bookstore.Analytics.Reports;
using Bookstore.Analytics.SearchConsole;
using Microsoft.Data.SqlClient;

namespace Bookstore.Analytics.Archive;

// Keeps Search Console data beyond the 16 months the UI and API offer.
public sealed class SqlServerArchive(string connectionString)
{
    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.SearchAnalyticsDaily') IS NULL
            CREATE TABLE dbo.SearchAnalyticsDaily (
                DataDate    date          NOT NULL,
                SearchType  varchar(16)   NOT NULL,
                Page        nvarchar(800) NOT NULL,
                Query       nvarchar(400) NOT NULL,
                Country     char(3)       NOT NULL,
                Device      varchar(10)   NOT NULL,
                Clicks      int           NOT NULL,
                Impressions int           NOT NULL,
                Position    float         NOT NULL,
                INDEX IX_SearchAnalyticsDaily_Date CLUSTERED (DataDate, SearchType));
            IF OBJECT_ID(N'dbo.SearchAnalyticsTotals') IS NULL
            CREATE TABLE dbo.SearchAnalyticsTotals (
                DataDate    date        NOT NULL,
                SearchType  varchar(16) NOT NULL,
                Clicks      int         NOT NULL,
                Impressions int         NOT NULL,
                Position    float       NOT NULL,
                CONSTRAINT PK_SearchAnalyticsTotals PRIMARY KEY (DataDate, SearchType));
            """;
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    // Idempotent: running the job twice for the same day leaves one copy.
    public async Task ReplaceDayAsync(DailyTotal total, IReadOnlyList<SearchAnalyticsRow> rows, CancellationToken ct = default)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlTransaction tx = (SqlTransaction)await connection.BeginTransactionAsync(ct);

        await using (SqlCommand delete = new("""
            DELETE dbo.SearchAnalyticsDaily  WHERE DataDate = @d AND SearchType = @t;
            DELETE dbo.SearchAnalyticsTotals WHERE DataDate = @d AND SearchType = @t;
            INSERT dbo.SearchAnalyticsTotals VALUES (@d, @t, @c, @i, @p);
            """, connection, tx))
        {
            delete.Parameters.Add("@d", SqlDbType.Date).Value = total.Date.ToDateTime(TimeOnly.MinValue);
            delete.Parameters.Add("@t", SqlDbType.VarChar, 16).Value = total.SearchType;
            delete.Parameters.Add("@c", SqlDbType.Int).Value = total.Clicks;
            delete.Parameters.Add("@i", SqlDbType.Int).Value = total.Impressions;
            delete.Parameters.Add("@p", SqlDbType.Float).Value = total.Position;
            await delete.ExecuteNonQueryAsync(ct);
        }

        using DataTable table = ToTable(rows);
        using SqlBulkCopy bulk = new(connection, SqlBulkCopyOptions.Default, tx)
        {
            DestinationTableName = "dbo.SearchAnalyticsDaily",
        };
        await bulk.WriteToServerAsync(table, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<List<PageWeek>> PageTotalsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Page, SUM(Clicks), SUM(Impressions)
            FROM dbo.SearchAnalyticsDaily
            WHERE SearchType = 'web' AND DataDate BETWEEN @from AND @to
            GROUP BY Page;
            """;
        List<PageWeek> result = [];
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@from", SqlDbType.Date).Value = from.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@to", SqlDbType.Date).Value = to.ToDateTime(TimeOnly.MinValue);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new PageWeek(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));
        }

        return result;
    }

    public async Task<int> SiteClicksAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using SqlCommand command = new("""
            SELECT COALESCE(SUM(Clicks), 0) FROM dbo.SearchAnalyticsTotals
            WHERE SearchType = 'web' AND DataDate BETWEEN @from AND @to;
            """, connection);
        command.Parameters.Add("@from", SqlDbType.Date).Value = from.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@to", SqlDbType.Date).Value = to.ToDateTime(TimeOnly.MinValue);
        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    private static DataTable ToTable(IReadOnlyList<SearchAnalyticsRow> rows)
    {
        DataTable table = new();
        table.Columns.Add("DataDate", typeof(DateTime));
        table.Columns.Add("SearchType", typeof(string));
        table.Columns.Add("Page", typeof(string));
        table.Columns.Add("Query", typeof(string));
        table.Columns.Add("Country", typeof(string));
        table.Columns.Add("Device", typeof(string));
        table.Columns.Add("Clicks", typeof(int));
        table.Columns.Add("Impressions", typeof(int));
        table.Columns.Add("Position", typeof(double));
        foreach (SearchAnalyticsRow r in rows)
        {
            table.Rows.Add(r.Date.ToDateTime(TimeOnly.MinValue), r.SearchType, r.Page, r.Query,
                r.Country, r.Device, r.Clicks, r.Impressions, r.Position);
        }

        return table;
    }
}
