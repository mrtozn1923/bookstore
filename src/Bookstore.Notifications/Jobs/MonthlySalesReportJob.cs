using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookstore.Notifications.Jobs;

public interface ISalesReportSource
{
    Task<MonthlySalesReport> GetAsync(DateOnly month, CancellationToken cancellationToken);
}

// Recurring on the 1st of every month for the previous month; can also be enqueued for any month
public sealed class MonthlySalesReportJob<TContext>(
    ISalesReportSource reports,
    IDocumentRenderer<MonthlySalesReport> renderer,
    IFileStore files,
    EmailOutbox outbox,
    TContext db,
    TimeProvider time,
    ILogger<MonthlySalesReportJob<TContext>> logger) where TContext : DbContext
{
    public static string PathFor(DateOnly month) => $"reports/monthly-sales-{month:yyyy-MM}.xlsx";

    // month is null for the recurring run: the previous calendar month in Istanbul time
    public async Task<string> RunAsync(string? month, CancellationToken cancellationToken)
    {
        DateOnly target = month is null
            ? PreviousMonth(TimeZoneInfo.ConvertTime(time.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")))
            : DateOnly.ParseExact(month + "-01", "yyyy-MM-dd");
        string path = PathFor(target);

        // Key: the file path; a retried or repeated run reuses the stored workbook
        if (!await files.ExistsAsync(path, cancellationToken))
        {
            MonthlySalesReport report = await reports.GetAsync(target, cancellationToken);
            using MemoryStream buffer = new();
            await renderer.RenderAsync(report, buffer, cancellationToken);
            buffer.Position = 0;
            await files.SaveAsync(path, buffer, renderer.Format.ContentType, cancellationToken);
        }

        // Key: "monthly-sales:{yyyy-MM}"; one announcement per month however often the job runs
        string url = $"{OrderDocumentJobs<TContext>.AppBaseUrl}/back-office/reports/monthly-sales/{target:yyyy-MM}";
        bool queued = await outbox.EnqueueAsync(db, $"monthly-sales:{target:yyyy-MM}", new EmailMessage
        {
            To = new EmailAddress("reports@example.com", "Bookstore back office"),
            Subject = $"Monthly sales report {target:yyyy-MM} is ready",
            HtmlBody = $"<p>The report is ready: <a href=\"{url}\">{url}</a></p>",
            TextBody = $"The report is ready: {url}",
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Monthly sales report {Month}: {Path}, email {State}", $"{target:yyyy-MM}", path, queued ? "queued" : "already queued");
        return path;
    }

    private static DateOnly PreviousMonth(DateTimeOffset local) => new DateOnly(local.Year, local.Month, 1).AddMonths(-1);
}
