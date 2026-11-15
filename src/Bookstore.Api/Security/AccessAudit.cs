using System.Diagnostics;
using Bookstore.Data;
using Bookstore.Institutional;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Security;

public sealed partial class AccessAudit(BookstoreContext db, TimeProvider clock, ILogger<AccessAudit> logger)
{
    public async Task WriteAsync(InstitutionalCaller caller, string action, int resourceInstitutionId,
        IEnumerable<int> resourceIds, string outcome, CancellationToken ct)
    {
        AccessAuditEntry entry = new()
        {
            OccurredAtUtc = clock.GetUtcNow().UtcDateTime,
            UserId = caller.UserId,
            CallerInstitutionId = caller.InstitutionId,
            Action = action,
            ResourceInstitutionId = resourceInstitutionId,
            ResourceIds = string.Join(",", resourceIds),
            Outcome = outcome,
            TraceId = Activity.Current?.TraceId.ToString()
        };
        db.AccessAuditEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        if (outcome == "denied")
        {
            LogCrossTenantDenied(caller.UserId, caller.InstitutionId, resourceInstitutionId, action);
        }
    }

    [LoggerMessage(EventId = 4031, Level = LogLevel.Warning,
        Message = "Cross-tenant access denied: user {UserId} of institution {CallerInstitutionId} asked for institution {ResourceInstitutionId} ({Action})")]
    private partial void LogCrossTenantDenied(string userId, int callerInstitutionId, int resourceInstitutionId, string action);
}

public sealed record CrossInstitutionRead(
    int ResourceInstitutionId, string UserId, int CallerInstitutionId, int Requests,
    DateTime FirstAtUtc, DateTime LastAtUtc, IReadOnlyList<int> OrderIds);

public static class IncidentQueries
{
    // Who read which institution's orders although they belong to another institution: built from the audit log,
    // never from guesses. Only "allowed" rows count: a denied request returned no data.
    public static async Task<IReadOnlyList<CrossInstitutionRead>> CrossInstitutionReadsAsync(
        BookstoreContext db, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        List<AccessAuditEntry> rows = await db.AccessAuditEntries.AsNoTracking()
            .Where(e => e.Action == "institution.orders.list" && e.Outcome == "allowed"
                && e.CallerInstitutionId != e.ResourceInstitutionId
                && e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc < toUtc)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(ct);

        return
        [
            .. rows.GroupBy(e => (e.ResourceInstitutionId, e.UserId, e.CallerInstitutionId))
                .Select(g => new CrossInstitutionRead(
                    g.Key.ResourceInstitutionId, g.Key.UserId, g.Key.CallerInstitutionId, g.Count(),
                    g.First().OccurredAtUtc, g.Last().OccurredAtUtc,
                    [.. g.SelectMany(e => e.ResourceIds.Split(',', StringSplitOptions.RemoveEmptyEntries)).Select(int.Parse).Distinct().Order()]))
                .OrderBy(r => r.ResourceInstitutionId).ThenBy(r => r.UserId)
        ];
    }
}
