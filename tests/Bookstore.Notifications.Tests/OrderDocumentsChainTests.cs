using System.Net.Http.Json;
using Azure.Storage.Blobs;
using Bookstore.Api.Data;
using Bookstore.Api.Jobs;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Jobs;
using Bookstore.Notifications.Outbox;
using Bookstore.Notifications.Storage;
using MailKit.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace Bookstore.Notifications.Tests;

// The chain's steps against real infrastructure: Azurite for the PDF, Mailpit for the email.
// Hangfire is only the scheduler, so the test calls the steps directly, each of them twice.
public sealed class OrderDocumentsChainTests
{
    private sealed record MailpitSummary(string ID, string Subject, int Attachments);
    private sealed record MailpitSearch(int Total, MailpitSummary[] Messages);
    private sealed record MailpitPart(string PartID, string FileName, string ContentType);
    private sealed record MailpitMessage(string Text, MailpitPart[] Attachments);

    [Fact]
    public async Task Order_placed_twice_gives_one_invoice_and_one_email_with_the_pdf()
    {
        string? mailpitApi = Environment.GetEnvironmentVariable("MAILPIT_API_URL");
        string? azurite = Environment.GetEnvironmentVariable("AZURITE_CONNECTION_STRING");
        Assert.SkipWhen(mailpitApi is null || azurite is null, "MAILPIT_API_URL and AZURITE_CONNECTION_STRING are needed");
        CancellationToken ct = TestContext.Current.CancellationToken;
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        int orderId = Random.Shared.Next(100_000, 999_999);

        BlobContainerClient container = new(azurite, "bookstore-chain-tests");
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        AzureBlobFileStore files = new(container, TimeProvider.System);
        await using SqliteConnection database = Keep(new SqliteConnection("Data Source=:memory:"));
        await using ServiceProvider services = new ServiceCollection()
            .AddDbContext<BookstoreContext>(o => o.UseSqlite(database))
            .BuildServiceProvider();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await db.Database.EnsureCreatedAsync(ct);

        IOptions<EmailOutboxOptions> outboxOptions = Options.Create(new EmailOutboxOptions { RetryDelays = EmailOutboxOptions.DefaultRetryDelays });
        OrderDocumentJobs<BookstoreContext> jobs = new(new SampleInvoiceSource(), new InvoiceArchive(new InvoicePdfRenderer(), files),
            new EmailOutbox(TimeProvider.System, outboxOptions), db, NullLogger<OrderDocumentJobs<BookstoreContext>>.Instance);

        for (int run = 0; run < 2; run++)
        {
            await jobs.GenerateInvoiceAsync(orderId, ct);
            await jobs.QueueConfirmationEmailAsync(orderId, ct);
        }

        await using (MailKitEmailSender sender = new(Options.Create(MailpitSmtp()), NullLogger<MailKitEmailSender>.Instance))
        {
            OutboxEmailDispatcher<BookstoreContext> dispatcher = new(services.GetRequiredService<IServiceScopeFactory>(), sender, files,
                TimeProvider.System, outboxOptions, NullLogger<OutboxEmailDispatcher<BookstoreContext>>.Instance);
            Assert.Equal(1, await dispatcher.DispatchDueAsync(ct));
            Assert.Equal(0, await dispatcher.DispatchDueAsync(ct));
        }

        string invoicePath = $"invoices/2026/BKS-2026-{orderId:D6}.pdf";
        Assert.True(await files.ExistsAsync(invoicePath, ct));
        Assert.Single(await ToListAsync(container.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.None, Azure.Storage.Blobs.Models.BlobStates.None, invoicePath, ct)));

        using HttpClient mailpit = new() { BaseAddress = new Uri(mailpitApi!) };
        MailpitSearch? found = await mailpit.GetFromJsonAsync<MailpitSearch>(
            $"/api/v1/search?query={Uri.EscapeDataString($"subject:\"order #{orderId} \"")}", ct);
        MailpitSummary summary = Assert.Single(found!.Messages);
        MailpitMessage? message = await mailpit.GetFromJsonAsync<MailpitMessage>($"/api/v1/message/{summary.ID}", ct);
        MailpitPart attachment = Assert.Single(message!.Attachments);
        Assert.Equal($"invoice-BKS-2026-{orderId:D6}.pdf", attachment.FileName);
        Assert.Contains($"https://bookstore.example.com/orders/{orderId}/invoice", message.Text);

        // The attachment is the stored PDF: its text has the order number, the Turkish address and the total
        byte[] pdfBytes = await mailpit.GetByteArrayAsync($"/api/v1/message/{summary.ID}/part/{attachment.PartID}", ct);
        using PdfDocument pdf = PdfDocument.Open(pdfBytes);
        string text = string.Join(" ", pdf.GetPage(1).GetWords().Select(w => w.Text));
        Assert.Contains($"Order #{orderId}", text);
        Assert.Contains("34710 İstanbul, Türkiye", text);
        Assert.Contains("Grand total ₺1.702,00", text);
    }

    private static SmtpOptions MailpitSmtp() => new()
    {
        Host = "127.0.0.1",
        Port = int.Parse(Environment.GetEnvironmentVariable("MAILPIT_SMTP_PORT") ?? "1025"),
        Security = SecureSocketOptions.StartTls,
        UserName = "bookstore",
        Password = Environment.GetEnvironmentVariable("Notifications__Smtp__Password"),
        TrustedCertificateSha256 = Environment.GetEnvironmentVariable("Notifications__Smtp__TrustedCertificateSha256"),
        FromAddress = "orders@example.com",
    };

    // An in-memory SQLite database lives as long as its connection is open
    private static SqliteConnection Keep(SqliteConnection connection)
    {
        connection.Open();
        return connection;
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> items)
    {
        List<T> list = [];
        await foreach (T item in items)
        {
            list.Add(item);
        }

        return list;
    }
}
