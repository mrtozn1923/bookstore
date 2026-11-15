using System.Globalization;
using System.Net;
using System.Text;
using Bookstore.Notifications.Documents;

namespace Bookstore.Notifications.Email;

// Hand-built HTML for post 02; post 03 replaces it with a Razor template
public static class OrderConfirmationEmail
{
    public static EmailMessage Create(InvoiceModel invoice, ReadOnlyMemory<byte> logoPng, EmailAttachment invoiceFile)
    {
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        StringBuilder rows = new();
        StringBuilder text = new();
        foreach (InvoiceLine line in invoice.Lines)
        {
            rows.Append($"<tr><td>{WebUtility.HtmlEncode(line.Title)}</td><td>{line.Quantity}</td><td>{line.NetAmount.ToString("C", tr)}</td></tr>");
            text.AppendLine($"{line.Quantity} x {line.Title}: {line.NetAmount.ToString("C", tr)}");
        }

        string total = invoice.GrandTotal.ToString("C", tr);
        return new EmailMessage
        {
            To = new EmailAddress(invoice.CustomerEmail, invoice.BillingAddress.Name),
            Subject = $"Your Bookstore order #{invoice.OrderId} is confirmed",
            HtmlBody = $"""
                <img src="cid:bookstore-logo" width="240" height="64" alt="Bookstore">
                <p>Hello {WebUtility.HtmlEncode(invoice.BillingAddress.Name)}, thank you for your order #{invoice.OrderId}.</p>
                <table>{rows}</table>
                <p>Total incl. VAT: <b>{total}</b>. Your invoice is attached.</p>
                """,
            TextBody = $"Hello {invoice.BillingAddress.Name}, thank you for your order #{invoice.OrderId}.\n\n{text}\nTotal incl. VAT: {total}. Your invoice is attached.",
            Attachments =
            [
                new EmailAttachment("logo.png", "image/png", logoPng, ContentId: "bookstore-logo"),
                invoiceFile,
            ],
        };
    }
}
