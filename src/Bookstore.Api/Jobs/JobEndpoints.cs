using Bookstore.Api.Data;
using Bookstore.Notifications;
using Bookstore.Notifications.Jobs;
using Hangfire;
using Hangfire.Storage;

namespace Bookstore.Api.Jobs;

public sealed record MonthlySalesRequest(string Month);

public static class JobEndpoints
{
    public const string MonthlyReportId = "monthly-sales-report";

    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        // Called by the relay that reads OrderPlaced from the Ordering outbox; may be called twice for the same order
        app.MapPost("/api/orders/{orderId:int}/placed", (int orderId, IBackgroundJobClient jobs) =>
        {
            string invoiceJob = jobs.Enqueue<OrderDocumentJobs<BookstoreContext>>(j => j.GenerateInvoiceAsync(orderId, CancellationToken.None));
            // Runs only after the invoice job succeeded; a failed invoice job is retried on its own
            string emailJob = jobs.ContinueJobWith<OrderDocumentJobs<BookstoreContext>>(invoiceJob,
                j => j.QueueConfirmationEmailAsync(orderId, CancellationToken.None));
            return Results.Accepted($"/api/jobs/{emailJob}", new { invoiceJob, emailJob });
        });

        // Post 01's report endpoint, now durable: the job survives a restart in Hangfire's storage
        app.MapPost("/api/reports/monthly-sales", (MonthlySalesRequest request, IBackgroundJobClient jobs) =>
        {
            string jobId = jobs.Enqueue<MonthlySalesReportJob<BookstoreContext>>(j => j.RunAsync(request.Month, CancellationToken.None));
            return Results.Accepted($"/api/jobs/{jobId}", new { jobId });
        });

        // Runs the recurring job now (same as the dashboard's "Trigger now")
        app.MapPost("/api/reports/monthly-sales/recurring/trigger", (IRecurringJobManagerV2 recurring) =>
            Results.Ok(new { jobId = recurring.TriggerJob(MonthlyReportId) }));

        app.MapGet("/api/reports/monthly-sales/{month}/link", async (string month, IFileStore files, CancellationToken ct) =>
        {
            string path = MonthlySalesReportJob<BookstoreContext>.PathFor(DateOnly.ParseExact(month + "-01", "yyyy-MM-dd"));
            return await files.ExistsAsync(path, ct)
                ? Results.Ok(new { url = await files.GetDownloadUrlAsync(path, TimeSpan.FromMinutes(5), cancellationToken: ct) })
                : Results.NotFound();
        });

        app.MapGet("/api/jobs/{jobId}", (string jobId, JobStorage storage) =>
        {
            using IStorageConnection connection = storage.GetConnection();
            if (connection.GetJobData(jobId) is not { } job)
            {
                return Results.NotFound();
            }

            StateData? state = connection.GetStateData(jobId);
            return Results.Ok(new { jobId, job.State, Job = $"{job.Job.Type.Name.Split('`')[0]}.{job.Job.Method.Name}", state?.Reason });
        });

        return app;
    }
}
