using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Breach;

public sealed record AffectedCustomer(int CustomerId, string[] Categories);

public sealed record BreachAssessment(
    DateTime DetectedAtUtc,
    DateTime AuthorityDeadlineUtc,
    IReadOnlyList<AffectedCustomer> Affected,
    string[] Categories,
    string[] SpecialCategories);

public sealed class BreachScope(BookstoreContext db)
{
    // Kurul decision 2019/10: notify the Authority without delay and within 72 hours of learning about the breach.
    public static readonly TimeSpan AuthorityDeadline = TimeSpan.FromHours(72);

    // None of Bookstore's categories is special category data under Art. 6; the list is here so the notice keeps the distinction.
    private static readonly HashSet<string> Special = ["health", "biometric"];

    public async Task<BreachAssessment> AssessAsync(string actorId, DateTime fromUtc, DateTime toUtc, DateTime detectedAtUtc, CancellationToken ct = default)
    {
        var rows = await db.Set<PersonalDataAccess>().AsNoTracking()
            .Where(a => a.ActorId == actorId && a.AtUtc >= fromUtc && a.AtUtc < toUtc)
            .Select(a => new { a.CustomerId, a.DataCategory })
            .Distinct()
            .ToListAsync(ct);

        List<AffectedCustomer> affected = [.. rows
            .GroupBy(r => r.CustomerId)
            .OrderBy(g => g.Key)
            .Select(g => new AffectedCustomer(g.Key, [.. g.Select(r => r.DataCategory).Order()]))];
        string[] categories = [.. rows.Select(r => r.DataCategory).Distinct().Order()];

        return new BreachAssessment(detectedAtUtc, detectedAtUtc + AuthorityDeadline, affected,
            [.. categories.Where(c => !Special.Contains(c))], [.. categories.Where(Special.Contains)]);
    }
}
