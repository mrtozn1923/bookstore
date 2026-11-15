using System.Collections.Concurrent;
using Bookstore.Notifications;

namespace Bookstore.Notifications.Tests;

// The abstraction pays off here: tests check what would be sent without any SMTP server
public sealed class FakeEmailSender : IEmailSender
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
