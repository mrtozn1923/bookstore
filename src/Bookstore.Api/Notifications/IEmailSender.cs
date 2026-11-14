namespace Bookstore.Notifications;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

public sealed class SmtpOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = 25;
    public required string From { get; init; }
}
