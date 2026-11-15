using Bookstore.Data;
using Bookstore.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Privacy.Consent;

public enum MessageKind { Transactional, Commercial }

public sealed record EmailPermission(bool Granted);

// A consent or rejection that must reach İYS within three business days.
[DataCategory("commercial-message")]
public sealed class IysSyncItem
{
    [NoDataClassification]
    public long Id { get; set; }

    [PersonalData(InformationType = "Contact Info")]
    public required string Address { get; set; }

    [NoDataClassification]
    public required string Channel { get; set; }

    [NoDataClassification]
    public bool Granted { get; set; }

    [NoDataClassification]
    public DateTime RecordedAtUtc { get; set; }

    [NoDataClassification]
    public DateOnly DueDate { get; set; }

    [NoDataClassification]
    public DateTime? SentAtUtc { get; set; }
}

// Official holidays come from configuration; the list changes every year.
public sealed record HolidayCalendar(IReadOnlySet<DateOnly> Days);

public sealed class CommercialMessages(BookstoreContext db, ConsentService consents, TimeProvider clock, HolidayCalendar holidays)
{
    // Order and delivery notices need no consent; a campaign e-mail does.
    public async Task<bool> CanSendAsync(int customerId, MessageKind kind, CancellationToken ct = default) =>
        kind == MessageKind.Transactional || await consents.HasConsentAsync(customerId, ConsentCatalog.CommercialEmail, ct);

    // Consent and rejection both go to İYS; the e-mail stops at once, not after the sync.
    public async Task SetEmailPermissionAsync(int customerId, bool granted, string source, CancellationToken ct = default)
    {
        Customer customer = await db.Customers.SingleAsync(c => c.Id == customerId, ct);
        DateTime now = clock.GetUtcNow().UtcDateTime;
        db.Add(new ConsentRecord
        {
            CustomerId = customerId,
            Purpose = ConsentCatalog.CommercialEmail,
            Granted = granted,
            NoticeVersion = ConsentCatalog.NoticeVersion,
            Source = source,
            RecordedAtUtc = now,
        });
        db.Add(new IysSyncItem
        {
            Address = customer.Email,
            Channel = "EPOSTA",
            Granted = granted,
            RecordedAtUtc = now,
            DueDate = BusinessDays.Add(DateOnly.FromDateTime(now), 3, holidays.Days),
        });
        await db.SaveChangesAsync(ct);
    }
}
