using System.Net;
using Bookstore.Data;
using Bookstore.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Api.Tests;

// Incident INC-2026-11-12: one institution could list another institution's orders.
public sealed class InstitutionOrdersIncidentTests(BookstoreApiFactory factory)
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    [Trait("incident", "INC-2026-11-12")]
    [Trait("asvs", "v5.0.0-8.4.1")]
    [Trait("asvs", "v5.0.0-16.3.2")]
    public async Task Another_institutions_order_list_is_not_found_and_the_attempt_is_audited()
    {
        HttpResponseMessage response = await factory.CreateClientAs(TestUsers.Alan).GetAsync("/api/institutions/1/orders", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AccessAuditEntry? entry = await LastAuditEntryAsync("alan");
        Assert.Equal(("denied", 2, 1), (entry?.Outcome, entry?.CallerInstitutionId, entry?.ResourceInstitutionId));
    }

    [Fact]
    [Trait("incident", "INC-2026-11-12")]
    public async Task Own_institution_order_list_is_returned_and_audited()
    {
        HttpResponseMessage response = await factory.CreateClientAs(TestUsers.Grace).GetAsync("/api/institutions/1/orders", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AccessAuditEntry? entry = await LastAuditEntryAsync("grace");
        Assert.Equal(("allowed", 1, 1), (entry?.Outcome, entry?.CallerInstitutionId, entry?.ResourceInstitutionId));
    }

    [Fact]
    [Trait("incident", "INC-2026-11-12")]
    public async Task Affected_records_come_from_allowed_cross_institution_reads_only()
    {
        DateTime day = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
            db.AccessAuditEntries.AddRange(
                Entry(day.AddHours(9), "grace", 1, 1, "4,7", "allowed"),    // own institution: not affected
                Entry(day.AddHours(10), "alan", 2, 1, "4,7", "allowed"),    // cross-institution read
                Entry(day.AddHours(11), "alan", 2, 1, "4,7,9", "allowed"),
                Entry(day.AddHours(12), "alan", 2, 1, "", "denied"));       // returned no data
            await db.SaveChangesAsync(ct);
        }

        await using AsyncServiceScope query = factory.Services.CreateAsyncScope();
        IReadOnlyList<CrossInstitutionRead> reads = await IncidentQueries.CrossInstitutionReadsAsync(
            query.ServiceProvider.GetRequiredService<BookstoreContext>(), day, day.AddDays(1), ct);

        CrossInstitutionRead read = Assert.Single(reads);
        Assert.Equal((1, "alan", 2), (read.ResourceInstitutionId, read.UserId, read.Requests));
        Assert.Equal([4, 7, 9], read.OrderIds);
    }

    private static AccessAuditEntry Entry(DateTime at, string user, int caller, int resource, string ids, string outcome) =>
        new()
        {
            OccurredAtUtc = at, UserId = user, CallerInstitutionId = caller, Action = "institution.orders.list",
            ResourceInstitutionId = resource, ResourceIds = ids, Outcome = outcome
        };

    private async Task<AccessAuditEntry?> LastAuditEntryAsync(string userId)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        return await db.AccessAuditEntries.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(ct);
    }
}
