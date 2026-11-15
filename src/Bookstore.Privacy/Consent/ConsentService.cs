using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Consent;

public sealed record CookieConsentDecision(Guid VisitorId, string NoticeVersion, Dictionary<string, bool> Choices);

public enum ConsentResult { Recorded, UnknownPurpose, StaleNotice }

public sealed class ConsentService(BookstoreContext db, TimeProvider clock)
{
    public async Task<ConsentResult> RecordCookieChoicesAsync(CookieConsentDecision decision, int? customerId, CancellationToken ct = default)
    {
        // A banner rendered with an older notice must ask again; the record has to point to the text the person saw.
        if (decision.NoticeVersion != ConsentCatalog.NoticeVersion)
        {
            return ConsentResult.StaleNotice;
        }
        if (decision.Choices.Keys.Any(k => !ConsentCatalog.IsCookiePurposeWithConsent(k)))
        {
            return ConsentResult.UnknownPurpose;
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        foreach ((string purpose, bool granted) in decision.Choices)
        {
            db.Add(new ConsentRecord
            {
                CustomerId = customerId,
                VisitorId = decision.VisitorId,
                Purpose = purpose,
                Granted = granted,
                NoticeVersion = decision.NoticeVersion,
                Source = "cookie-banner",
                RecordedAtUtc = now,
            });
        }
        await db.SaveChangesAsync(ct);
        return ConsentResult.Recorded;
    }

    // The latest row per purpose wins; no row means no consent.
    public async Task<bool> HasConsentAsync(int customerId, string purpose, CancellationToken ct = default) =>
        await db.Set<ConsentRecord>()
            .Where(r => r.CustomerId == customerId && r.Purpose == purpose)
            .OrderByDescending(r => r.RecordedAtUtc).ThenByDescending(r => r.Id)
            .Select(r => (bool?)r.Granted)
            .FirstOrDefaultAsync(ct) ?? false;
}
