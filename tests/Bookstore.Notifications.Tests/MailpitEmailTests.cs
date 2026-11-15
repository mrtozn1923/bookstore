using System.Net.Http.Json;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Samples;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Tests;

// Runs against a local Mailpit; skipped when MAILPIT_API_URL is not set
public sealed class MailpitEmailTests
{
    private sealed record MailpitAddress(string Name, string Address);
    private sealed record MailpitSummary(string ID, string Subject, MailpitAddress[] To, int Attachments);
    private sealed record MailpitSearch(int Total, MailpitSummary[] Messages);
    private sealed record MailpitPart(string FileName, string ContentType, string? ContentID);
    private sealed record MailpitMessage(string Text, string HTML, MailpitPart[] Inline, MailpitPart[] Attachments);

    [Fact]
    public async Task Order_confirmation_arrives_with_inline_logo_and_attached_invoice()
    {
        string? api = Environment.GetEnvironmentVariable("MAILPIT_API_URL");
        Assert.SkipWhen(api is null, "MAILPIT_API_URL is not set");
        CancellationToken ct = TestContext.Current.CancellationToken;

        SmtpOptions options = new()
        {
            Host = "127.0.0.1",
            Port = int.Parse(Environment.GetEnvironmentVariable("MAILPIT_SMTP_PORT") ?? "1025"),
            Security = SecureSocketOptions.StartTls,
            UserName = "bookstore",
            Password = Environment.GetEnvironmentVariable("Notifications__Smtp__Password"),
            TrustedCertificateSha256 = Environment.GetEnvironmentVariable("Notifications__Smtp__TrustedCertificateSha256"),
            FromAddress = "orders@example.com",
        };

        InvoiceModel invoice = BookstoreSampleData.Invoice(1042);
        EmailMessage message = OrderConfirmationEmail.Create(invoice, new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            new EmailAttachment("invoice.txt", "text/plain", "Dune x2"u8.ToArray()));
        message = message with { Subject = $"{message.Subject} [{Guid.NewGuid():N}]" };

        await using (MailKitEmailSender sender = new(Options.Create(options), NullLogger<MailKitEmailSender>.Instance))
        {
            await sender.SendAsync(message, ct);
        }

        using HttpClient mailpit = new() { BaseAddress = new Uri(api!) };
        MailpitSearch? found = await mailpit.GetFromJsonAsync<MailpitSearch>(
            $"/api/v1/search?query={Uri.EscapeDataString($"subject:\"{message.Subject}\"")}", ct);

        MailpitSummary summary = Assert.Single(found!.Messages);
        Assert.Equal("ada@example.com", Assert.Single(summary.To).Address);

        MailpitMessage? full = await mailpit.GetFromJsonAsync<MailpitMessage>($"/api/v1/message/{summary.ID}", ct);
        Assert.Equal("bookstore-logo", Assert.Single(full!.Inline).ContentID);
        Assert.Equal("invoice.txt", Assert.Single(full.Attachments).FileName);
        Assert.Contains("cid:bookstore-logo", full.HTML);
        Assert.Contains("Total incl. VAT: ₺1.702,00", full.Text);
    }
}
