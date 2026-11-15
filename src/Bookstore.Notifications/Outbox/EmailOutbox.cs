using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Outbox;

public sealed class EmailOutboxOptions
{
    public string SenderDomain { get; set; } = "example.com";
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 20;

    // Delay before attempt 2, 3, ...; the email fails for good after the last one.
    // Left empty here: the configuration binder appends to an initialized array instead of replacing it
    public TimeSpan[] RetryDelays { get; set; } = [];

    public static readonly TimeSpan[] DefaultRetryDelays =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), TimeSpan.FromHours(6)];
}

// Adds the email to the caller's DbContext; it is sent only if the caller's SaveChanges commits
public sealed class EmailOutbox(TimeProvider time, IOptions<EmailOutboxOptions> options)
{
    public async Task<bool> EnqueueAsync(DbContext db, string idempotencyKey, EmailMessage message, CancellationToken cancellationToken = default) =>
        await EnqueueAsync(db, idempotencyKey, message, attachment: null, cancellationToken);

    public async Task<bool> EnqueueAsync(DbContext db, string idempotencyKey, EmailMessage message, StoredAttachment? attachment,
        CancellationToken cancellationToken = default)
    {
        if (await db.Set<OutboxEmail>().AnyAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return false;
        }

        db.Set<OutboxEmail>().Add(OutboxEmail.From(idempotencyKey, message, options.Value.SenderDomain, time.GetUtcNow().UtcDateTime, attachment));
        return true;
    }
}
