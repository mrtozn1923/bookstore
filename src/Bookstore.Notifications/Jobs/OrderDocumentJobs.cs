using System.Globalization;
using System.Net;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bookstore.Notifications.Jobs;

// Loads the issued invoice of an order; Bookstore reads it from the Ordering module
public interface IInvoiceSource
{
    Task<InvoiceModel> GetAsync(int orderId, CancellationToken cancellationToken);
}

// OrderPlaced -> step 1: invoice PDF in the file store -> step 2: confirmation email in the outbox -> dispatcher sends it.
// Every step may run more than once (retries, a relay that enqueues twice); each has its own idempotency key.
public sealed class OrderDocumentJobs<TContext>(
    IInvoiceSource invoices,
    InvoiceArchive archive,
    EmailOutbox outbox,
    TContext db,
    ILogger<OrderDocumentJobs<TContext>> logger) where TContext : DbContext
{
    public const string AppBaseUrl = "https://bookstore.example.com";

    // Key: the file path invoices/{year}/{invoiceNumber}.pdf; a second run finds the file and renders nothing
    public async Task<string> GenerateInvoiceAsync(int orderId, CancellationToken cancellationToken)
    {
        InvoiceModel invoice = await invoices.GetAsync(orderId, cancellationToken);
        bool existed = await archive.ExistsAsync(invoice, cancellationToken);
        string path = await archive.EnsureStoredAsync(invoice, cancellationToken);
        logger.LogInformation("Invoice for order {OrderId}: {Path} ({State})", orderId, path, existed ? "already stored" : "rendered");
        return path;
    }

    // Key: "order-confirmed:{orderId}" in the outbox; a second run finds the row and queues nothing
    public async Task QueueConfirmationEmailAsync(int orderId, CancellationToken cancellationToken)
    {
        InvoiceModel invoice = await invoices.GetAsync(orderId, cancellationToken);
        string path = InvoiceArchive.PathFor(invoice);
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        string name = WebUtility.HtmlEncode(invoice.BillingAddress.Name);

        // The link points to the app, which checks the customer and hands out a 5-minute storage URL
        string invoiceUrl = $"{AppBaseUrl}/orders/{orderId}/invoice";
        EmailMessage message = new()
        {
            To = new EmailAddress(invoice.CustomerEmail, invoice.BillingAddress.Name),
            Subject = $"Your Bookstore order #{orderId} is confirmed",
            HtmlBody = $"""
                <p>Hello {name}, thank you for your order #{orderId}.</p>
                <p>Total incl. VAT: <b>{invoice.GrandTotal.ToString("C", tr)}</b>. Your invoice {invoice.InvoiceNumber} is attached
                and always available at <a href="{invoiceUrl}">{invoiceUrl}</a>.</p>
                """,
            TextBody = $"Hello {invoice.BillingAddress.Name}, thank you for your order #{orderId}.\nTotal incl. VAT: {invoice.GrandTotal.ToString("C", tr)}. Your invoice {invoice.InvoiceNumber} is attached and always available at {invoiceUrl}.",
        };

        bool queued = await outbox.EnqueueAsync(db, $"order-confirmed:{orderId}", message,
            new StoredAttachment(path, $"invoice-{invoice.InvoiceNumber}.pdf", FileFormat.Pdf.ContentType), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Confirmation email for order {OrderId}: {State}", orderId, queued ? "queued" : "already queued");
    }
}
