using System.Text.Json.Serialization;
using Bookstore.Api.Catalog;
using Bookstore.Api.Data;
using Bookstore.Api.Institutional;
using Bookstore.Api.Reports;
using Bookstore.Notifications;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Documents.Excel;
using Bookstore.Notifications.Documents.Html;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Outbox;
using Bookstore.Notifications.Samples;
using Microsoft.EntityFrameworkCore;

// Bookstore qualifies for the QuestPDF Community License (annual gross revenue under USD 1M); set once at startup
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddDbContext<BookstoreContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Bookstore")));
builder.Services.AddEmailOutbox<BookstoreContext>(builder.Configuration);
builder.Services.AddScoped<ApprovalReminderJob>();
builder.Services.AddSingleton<IDocumentRenderer<InvoiceModel>, InvoicePdfRenderer>();
builder.Services.AddSingleton<InvoiceArchive>();
builder.Services.AddSingleton<IDocumentRenderer<MonthlySalesReport>, MonthlySalesExcelRenderer>();
builder.Services.AddSingleton<ReportJobQueue>();
builder.Services.AddSingleton<ReportJobStore>();
builder.Services.AddHostedService<ReportJobWorker>();
builder.Services.AddSingleton<CoverThumbnailQueue>();
builder.Services.AddHostedService<CoverThumbnailWorker>();
builder.Services.AddRequestTimeouts();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

WebApplication app = builder.Build();
app.UseRequestTimeouts();
app.MapCoverEndpoints();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    // Demo only: create the SQLite schema and seed data on start
    await scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.EnsureCreatedAsync();
}

// The first download renders and stores the PDF; later downloads stream the stored file
app.MapGet("/api/orders/{orderId:int}/invoice", async (int orderId, InvoiceArchive archive, IFileStore files, CancellationToken ct) =>
{
    InvoiceModel invoice = BookstoreSampleData.Invoice(orderId);
    string path = await archive.EnsureStoredAsync(invoice, ct);
    Stream pdf = await files.OpenReadAsync(path, ct);
    return Results.File(pdf, FileFormat.Pdf.ContentType, $"invoice-{invoice.InvoiceNumber}.pdf");
});

// Designer's HTML/CSS quote printed by headless Chromium; the browser starts with the first quote
app.MapGet("/api/institutional/quotes/{quoteNumber}/pdf", async (string quoteNumber, IDocumentRenderer<InstitutionalQuoteModel> renderer, CancellationToken ct) =>
{
    InstitutionalQuoteModel quote = BookstoreSampleData.InstitutionalQuote() with { QuoteNumber = quoteNumber };
    MemoryStream pdf = new();
    await renderer.RenderAsync(quote, pdf, ct);
    pdf.Position = 0;
    return Results.File(pdf, renderer.Format.ContentType, $"quote-{quoteNumber}.pdf");
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

app.MapGet("/api/reports/{jobId:guid}/file", async (Guid jobId, ReportJobStore store, IFileStore files, IDocumentRenderer<MonthlySalesReport> renderer, CancellationToken ct) =>
{
    if (store.Get(jobId) is not { State: ReportJobState.Completed, FilePath: { } path })
    {
        return Results.NotFound();
    }

    Stream content = await files.OpenReadAsync(path, ct);
    return Results.Stream(content, renderer.Format.ContentType, Path.GetFileName(path));
});

// Excel straight into the response: ClosedXML writes synchronously, so render into memory first
app.MapGet("/api/reports/monthly-sales/{month}/excel", async (string month, IDocumentRenderer<MonthlySalesReport> renderer, CancellationToken ct) =>
{
    MonthlySalesReport report = BookstoreSampleData.MonthlySales(DateOnly.ParseExact(month + "-01", "yyyy-MM-dd"), rowCount: 2_000);
    MemoryStream buffer = new();
    await renderer.RenderAsync(report, buffer, ct);
    buffer.Position = 0;
    return Results.File(buffer, renderer.Format.ContentType, $"Aylık Satış Raporu {month}.xlsx");
});

// The wrong way, kept to show the error: writing the workbook directly to Response.Body
app.MapGet("/api/reports/monthly-sales/{month}/excel-direct", async (string month, HttpContext http, IDocumentRenderer<MonthlySalesReport> renderer) =>
{
    MonthlySalesReport report = BookstoreSampleData.MonthlySales(DateOnly.ParseExact(month + "-01", "yyyy-MM-dd"), rowCount: 2_000);
    http.Response.ContentType = renderer.Format.ContentType;
    await renderer.RenderAsync(report, http.Response.Body, http.RequestAborted);
});

// In production a daily recurring job calls this; exposed here to trigger it by hand
app.MapPost("/api/institutional/approval-reminders/run", (ApprovalReminderJob job, CancellationToken ct) => job.RunAsync(ct));

app.MapGet("/api/outbox", async (BookstoreContext db, CancellationToken ct) =>
    await db.Set<OutboxEmail>().OrderBy(e => e.CreatedAtUtc)
        .Select(e => new { e.IdempotencyKey, e.Status, e.Attempts, e.NextAttemptAtUtc, e.SentAtUtc, e.LastError })
        .ToListAsync(ct));

app.Run();

public sealed record MonthlySalesRequest(string Month, string NotifyEmail);
