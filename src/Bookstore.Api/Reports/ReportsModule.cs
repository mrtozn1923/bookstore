using System.Data.Common;
using System.Globalization;
using Bookstore.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Reports;

public sealed record CategoryRevenue(string Category, decimal Revenue);

public static class ReportsModule
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/reports/revenue", GetRevenue).WithTags("Reports");
        return app;
    }

    // GET /api/admin/reports/revenue?category=classics
    private static async Task<Ok<CategoryRevenue>> GetRevenue(string category, BookstoreContext db, CancellationToken ct)
    {
        DbConnection connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(i.LineTotal), 0)
            FROM ordering.OrderItems i
            JOIN catalog.Books b ON b.Id = i.BookId
            JOIN catalog.Categories c ON c.Id = b.CategoryId
            WHERE c.Slug = @category
            """;
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@category";
        parameter.Value = category;
        command.Parameters.Add(parameter);

        decimal revenue = Convert.ToDecimal(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
        return TypedResults.Ok(new CategoryRevenue(category, revenue));
    }
}
