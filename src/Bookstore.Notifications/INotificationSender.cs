namespace Bookstore.Notifications;

public sealed record EmailMessage(string To, string Subject, string Body);

public interface INotificationSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
