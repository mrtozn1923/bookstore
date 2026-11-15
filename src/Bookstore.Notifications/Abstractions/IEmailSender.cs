namespace Bookstore.Notifications;

public sealed record EmailAddress(string Address, string? DisplayName = null);

// ContentId set: an inline image referenced from the HTML body as cid:<ContentId>
public sealed record EmailAttachment(string FileName, string ContentType, ReadOnlyMemory<byte> Content, string? ContentId = null);

public sealed record EmailMessage
{
    public required EmailAddress To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required string TextBody { get; init; }
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];

    // Set by the outbox so a retried message keeps the same Message-Id
    public string? MessageId { get; init; }
}

// Thrown by senders; IsTransient tells the caller whether a retry can help
public sealed class EmailDeliveryException(string message, bool isTransient, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsTransient { get; } = isTransient;
}

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
