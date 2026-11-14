namespace Bookstore.Notifications.Contracts;

// Birçok modülün ortak kullandığı sözleşme: sahibi Notifications
public interface INotificationSender
{
    Task SendAsync(string to, string subject, CancellationToken ct = default);
}
