using System.Text.Json.Serialization;
using Bookstore.Api.Data;
using Bookstore.Api.Institutional;
using Bookstore.Api.Reports;
using Bookstore.Notifications;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Outbox;
using Bookstore.Notifications.Samples;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddDbContext<BookstoreContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Bookstore")));
builder.Services.AddEmailOutbox<BookstoreContext>(builder.Configuration);
builder.Services.AddScoped<ApprovalReminderJob>();
builder.Services.AddSingleton<IDocumentRenderer<InvoiceModel>, PlainTextInvoiceRenderer>();
builder.Services.AddSingleton<IDocumentRenderer<MonthlySalesReport>>(
    new PlainTextSalesReportRenderer(builder.Configuration.GetValue("Reports:SimulatedRenderTime", TimeSpan.FromSeconds(8))));
builder.Services.AddSingleton<ReportJobQueue>();
builder.Services.AddSingleton<ReportJobStore>();
builder.Services.AddHostedService<ReportJobWorker>();
builder.Services.AddRequestTimeouts();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

WebApplication app = builder.Build();
app.UseRequestTimeouts();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    // Demo only: create the SQLite schema and seed data on start
    await scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.EnsureCreatedAsync();
}

// Synchronous: small, fast document rendered straight into the response
app.MapGet("/api/orders/{orderId:int}/invoice", (int orderId, IDocumentRenderer<InvoiceModel> renderer, CancellationToken ct) =>
{
    InvoiceModel invoice = BookstoreSampleData.Invoice(orderId);
    return Results.Stream(
        stream => renderer.RenderAsync(invoice, stream, ct),
        renderer.Format.ContentType,
        fileDownloadName: $"invoice-{invoice.InvoiceNumber}{renderer.Format.Extension}");
});

// Synchronous on purpose, to show what a slow document does to a request
app.MapGet("/api/reports/monthly-sales/{month}", async (string month, IDocumentRenderer<MonthlySalesReport> renderer, CancellationToken ct) =>
{
    MonthlySalesReport report = BookstoreSampleData.MonthlySales(DateOnly.ParseExact(month + "-01", "yyyy-MM-dd"), rowCount: 2_000);
    using MemoryStream buffer = new();
    await renderer.RenderAsync(report, buffer, ct);
    return Results.File(buffer.ToArray(), renderer.Format.ContentType, $"monthly-sales-{month}{renderer.Format.Extension}");
}).WithRequestTimeout(TimeSpan.FromSeconds(5));

// Asynchronous: accept, queue, answer 202 with a status URL
app.MapPost("/api/reports/monthly-sales", async (MonthlySalesRequest request, ReportJobQueue queue, ReportJobStore store, CancellationToken ct) =>
{
    ReportJob job = new(Guid.CreateVersion7(), DateOnly.ParseExact(request.Month + "-01", "yyyy-MM-dd"), request.NotifyEmail);
    ReportJobStatus queued = new(job.Id, ReportJobState.Queued);
    store.Set(queued);
    await queue.EnqueueAsync(job, ct);
    return Results.Accepted($"/api/reports/{job.Id}", queued);
});

app.MapGet("/api/reports/{jobId:guid}", (Guid jobId, ReportJobStore store) =>
    store.Get(jobId) is { } status ? Results.Ok(status) : Results.NotFound());

app.MapGet("/api/reports/{jobId:guid}/file", async (Guid jobId, ReportJobStore store, IFileStore files, CancellationToken ct) =>
{
    if (store.Get(jobId) is not { State: ReportJobState.Completed, FilePath: { } path })
    {
        return Results.NotFound();
    }

    Stream content = await files.OpenReadAsync(path, ct);
    return Results.Stream(content, FileFormat.PlainText.ContentType, Path.GetFileName(path));
});

// In production a daily recurring job calls this; exposed here to trigger it by hand
app.MapPost("/api/institutional/approval-reminders/run", (ApprovalReminderJob job, CancellationToken ct) => job.RunAsync(ct));

app.MapGet("/api/outbox", async (BookstoreContext db, CancellationToken ct) =>
    await db.Set<OutboxEmail>().OrderBy(e => e.CreatedAtUtc)
        .Select(e => new { e.IdempotencyKey, e.Status, e.Attempts, e.NextAttemptAtUtc, e.SentAtUtc, e.LastError })
        .ToListAsync(ct));

app.Run();

public sealed record MonthlySalesRequest(string Month, string NotifyEmail);
