using Microsoft.EntityFrameworkCore;

namespace Bookstore.Notifications.Outbox;

public enum OutboxEmailStatus { Pending, Sent, Failed }

public sealed class OutboxEmail
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    // One logical email per key: "order-confirmed:1042", "approval-reminder:nevin@example.com:2026-11-15"
    public required string IdempotencyKey { get; init; }
    public required string MessageId { get; init; }
    public required string ToAddress { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required string TextBody { get; init; }

    public OutboxEmailStatus Status { get; set; } = OutboxEmailStatus.Pending;
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }

    public static OutboxEmail From(string idempotencyKey, EmailMessage message, string senderDomain, DateTime nowUtc) => new()
    {
        IdempotencyKey = idempotencyKey,
        // Same key -> same Message-Id, also on every retry
        MessageId = $"{Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(idempotencyKey)))[..32]}@{senderDomain}",
        ToAddress = message.To.Address,
        ToName = message.To.DisplayName,
        Subject = message.Subject,
        HtmlBody = message.HtmlBody,
        TextBody = message.TextBody,
        CreatedAtUtc = nowUtc,
        NextAttemptAtUtc = nowUtc,
    };

    public EmailMessage ToMessage() => new()
    {
        To = new EmailAddress(ToAddress, ToName),
        Subject = Subject,
        HtmlBody = HtmlBody,
        TextBody = TextBody,
        MessageId = MessageId,
    };
}

public static class OutboxModelBuilderExtensions
{
    public static ModelBuilder ApplyEmailOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxEmail>(e =>
        {
            e.ToTable("OutboxEmails");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        });
        return modelBuilder;
    }
}
