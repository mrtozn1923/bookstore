using MailKit;
using MailKit.Net.Smtp;
using MimeKit;
using StackExchange.Redis;

namespace Bookstore.Notifications.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string From { get; set; } = "Bookstore <no-reply@example.com>";
}

public interface IOrderEmails
{
    Task SendOnceAsync(Guid orderId, string kind, string to, string subject, string body, CancellationToken ct);
}

// RabbitMQ delivers at least once and Notifications has no inbox table, so the same event may arrive twice.
// A Redis key per order and mail type makes the e-mail go out once.
public sealed class OrderEmails(IConnectionMultiplexer redis, Microsoft.Extensions.Options.IOptions<SmtpOptions> options, ILogger<OrderEmails> logger)
    : IOrderEmails
{
    public async Task SendOnceAsync(Guid orderId, string kind, string to, string subject, string body, CancellationToken ct)
    {
        IDatabase db = redis.GetDatabase();
        string key = $"notifications:email:{orderId}:{kind}";
        // SET NX: only the first delivery gets the key. Kept for 7 days, much longer than any redelivery.
        if (!await db.StringSetAsync(key, "sending", TimeSpan.FromDays(7), When.NotExists))
        {
            logger.LogInformation("E-mail {Kind} for order {OrderId} already sent, skipping", kind, orderId);
            return;
        }

        try
        {
            SmtpOptions smtp = options.Value;
            MimeMessage message = new()
            {
                Subject = subject,
                Body = new TextPart("plain") { Text = body }
            };
            message.From.Add(MailboxAddress.Parse(smtp.From));
            message.To.Add(MailboxAddress.Parse(to));

            using SmtpClient client = new();
            await client.ConnectAsync(smtp.Host, smtp.Port, MailKit.Security.SecureSocketOptions.None, ct);
            // MailKit 5.0 takes a SendRequest instead of the message itself
            await client.SendAsync(new SendRequest(message), ct);
            await client.DisconnectAsync(true, ct);
            logger.LogInformation("E-mail {Kind} for order {OrderId} sent to {To}", kind, orderId, to);
        }
        catch
        {
            // Not sent: release the key so that Wolverine's retry can try again
            await db.KeyDeleteAsync(key);
            throw;
        }
    }
}
