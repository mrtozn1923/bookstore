using Microsoft.Extensions.Logging;

namespace Bookstore.Notifications.Email;

// Development-only sender: writes the e-mail to the log instead of sending it
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email to {To}: {Subject} ({Attachments} attachments)",
            message.To.Address, message.Subject, message.Attachments.Count);
        return Task.CompletedTask;
    }
}
