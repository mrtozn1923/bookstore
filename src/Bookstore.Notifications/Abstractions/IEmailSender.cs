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
}

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
