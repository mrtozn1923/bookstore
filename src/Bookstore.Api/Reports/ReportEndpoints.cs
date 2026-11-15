namespace Bookstore.Api.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        // Internal management report; put it behind an admin policy before it leaves development.
        app.MapPost("/api/reports/sales/{year:int}/{month:int}", async Task<IResult> (
            int year, int month, SalesReportService reports, CancellationToken ct) =>
        {
            SalesReport report = await reports.CreateAsync(year, month, ct);
            ReportFile? workbook = report.Files.FirstOrDefault(f => f.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
            return workbook is null
                ? TypedResults.Problem($"No workbook was produced. Summary: {report.Summary}", statusCode: StatusCodes.Status502BadGateway)
                : TypedResults.File(workbook.Content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", workbook.FileName);
        });

        return app;
    }
}
