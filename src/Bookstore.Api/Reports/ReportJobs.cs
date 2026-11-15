using System.Collections.Concurrent;
using System.Threading.Channels;
using Bookstore.Notifications;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Samples;

namespace Bookstore.Api.Reports;

public enum ReportJobState { Queued, Running, Completed, Failed }

public sealed record ReportJob(Guid Id, DateOnly Month, string NotifyEmail);

public sealed record ReportJobStatus(Guid Id, ReportJobState State, string? FilePath = null, string? Error = null);

public sealed class ReportJobQueue
{
    private readonly Channel<ReportJob> _channel = Channel.CreateBounded<ReportJob>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

    public ValueTask EnqueueAsync(ReportJob job, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<ReportJob> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class ReportJobStore
{
    private readonly ConcurrentDictionary<Guid, ReportJobStatus> _jobs = new();

    public void Set(ReportJobStatus status) => _jobs[status.Id] = status;

    public ReportJobStatus? Get(Guid id) => _jobs.TryGetValue(id, out ReportJobStatus? status) ? status : null;
}

public sealed class ReportJobWorker(
    ReportJobQueue queue,
    ReportJobStore store,
    IDocumentRenderer<MonthlySalesReport> renderer,
    IFileStore fileStore,
    IEmailSender emailSender,
    ILogger<ReportJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ReportJob job in queue.ReadAllAsync(stoppingToken))
        {
            store.Set(new ReportJobStatus(job.Id, ReportJobState.Running));
            try
            {
                MonthlySalesReport report = BookstoreSampleData.MonthlySales(job.Month, rowCount: 2_000);
                string path = $"reports/monthly-sales-{job.Month:yyyy-MM}-{job.Id:N}{renderer.Format.Extension}";

                using MemoryStream buffer = new();
                await renderer.RenderAsync(report, buffer, stoppingToken);
                buffer.Position = 0;
                StoredFile file = await fileStore.SaveAsync(path, buffer, renderer.Format.ContentType, stoppingToken);

                await emailSender.SendAsync(new EmailMessage
                {
                    To = new EmailAddress(job.NotifyEmail),
                    Subject = $"Monthly sales report {job.Month:yyyy-MM} is ready",
                    HtmlBody = $"<p>Your report is ready: <a href=\"/api/reports/{job.Id}/file\">download</a></p>",
                    TextBody = $"Your report is ready: /api/reports/{job.Id}/file",
                }, stoppingToken);

                store.Set(new ReportJobStatus(job.Id, ReportJobState.Completed, file.Path));
                logger.LogInformation("Report job {JobId} completed: {Path} ({Length} bytes)", job.Id, file.Path, file.Length);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one failed report stop the worker loop
                store.Set(new ReportJobStatus(job.Id, ReportJobState.Failed, Error: ex.Message));
                logger.LogError(ex, "Report job {JobId} failed", job.Id);
            }
        }
    }
}
