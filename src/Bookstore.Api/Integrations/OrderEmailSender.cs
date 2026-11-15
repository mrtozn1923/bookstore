using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bookstore.Api.Integrations;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string From { get; set; } = "orders@bookstore.test";
}

public interface IOrderEmailSender
{
    Task SendOrderConfirmationAsync(string to, string orderReference, decimal total, CancellationToken cancellationToken);
}

public sealed class SmtpOrderEmailSender(IOptions<SmtpOptions> options) : IOrderEmailSender
{
    public async Task SendOrderConfirmationAsync(string to, string orderReference, decimal total, CancellationToken cancellationToken)
    {
        SmtpOptions smtp = options.Value;
        MimeMessage message = new();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = $"Order {orderReference} confirmed";
        message.Body = new TextPart("plain") { Text = $"Thank you! Your order total is {total:0.00} TRY." };

        using SmtpClient client = new();
        await client.ConnectAsync(smtp.Host, smtp.Port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
