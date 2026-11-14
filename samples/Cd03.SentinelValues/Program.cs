using System.Text.Json;
using System.Text.Json.Serialization;
using Bookstore.Institutional.Sentinels;
using Microsoft.EntityFrameworkCore;

string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
var options = new DbContextOptionsBuilder<SentinelContext>().UseSqlServer(cs).Options;
JsonSerializerOptions Web = JsonSerializerOptions.Web;

await using (var setup = new SentinelContext(options))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    setup.InstitutionUsers.AddRange(
        new InstitutionUser { FullName = "Ada Lovelace", SpendingLimit = 85_000m, SpendingLimitStatus = SpendingLimitStatus.Set },
        new InstitutionUser { FullName = "Alan Turing", SpendingLimit = 92_000m, SpendingLimitStatus = SpendingLimitStatus.Set },
        new InstitutionUser { FullName = "Grace Hopper", SpendingLimit = 78_000m, SpendingLimitStatus = SpendingLimitStatus.Set },
        new InstitutionUser { FullName = "Linus Torvalds", SpendingLimitStatus = SpendingLimitStatus.Pending },
        new InstitutionUser { FullName = "Barbara Liskov", SpendingLimitStatus = SpendingLimitStatus.Pending });
    setup.DiscountAgreements.AddRange(
        new DiscountAgreement { InstitutionId = 1, DiscountRate = 0.10m, StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 2, 28) },
        new DiscountAgreement { InstitutionId = 2, DiscountRate = 0.10m, StartDate = new DateOnly(2025, 10, 1), EndDate = new DateOnly(2026, 3, 31) },
        new DiscountAgreement { InstitutionId = 3, DiscountRate = 0.10m, StartDate = new DateOnly(2026, 3, 23), EndDate = null });
    await setup.SaveChangesAsync();
}

await using var db = new SentinelContext(options);

Console.WriteLine("--- A. nullable column");
decimal? average = await db.InstitutionUsers.AverageAsync(u => u.SpendingLimit);
int pending = await db.InstitutionUsers.CountAsync(u => u.SpendingLimit == null);
Console.WriteLine($"Average spending limit: {average:0.##}, pending: {pending}");

Console.WriteLine("--- B. check constraint");
db.InstitutionUsers.Add(new InstitutionUser { FullName = "Edsger Dijkstra", SpendingLimitStatus = SpendingLimitStatus.Set });
try { await db.SaveChangesAsync(); }
catch (DbUpdateException ex) { Console.WriteLine(ex.InnerException?.Message); db.ChangeTracker.Clear(); }

Console.WriteLine("--- DateTime.MinValue into datetime");
db.LegacyContracts.Add(new LegacyContract { SignedAt = DateTime.MinValue });
try { await db.SaveChangesAsync(); }
catch (DbUpdateException ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}"); db.ChangeTracker.Clear(); }

Console.WriteLine("--- agreements in force");
var day = new DateOnly(2026, 4, 15);
var inForce = db.DiscountAgreements.Where(a => a.StartDate <= day && (a.EndDate == null || a.EndDate >= day));
Console.WriteLine(inForce.ToQueryString());
Console.WriteLine($"In force on {day:yyyy-MM-dd}: {await inForce.CountAsync()}");

var inForceFast = db.DiscountAgreements.Where(a => a.StartDate <= day && a.EffectiveEndDate >= day);
Console.WriteLine(inForceFast.ToQueryString());
Console.WriteLine($"In force (computed column): {await inForceFast.CountAsync()}");

foreach (var agreement in await db.DiscountAgreements.OrderBy(a => a.Id).ToListAsync())
    Console.WriteLine($"#{agreement.Id} {agreement.StartDate:yyyy-MM-dd} -> {agreement.End}, effective {agreement.EffectiveEndDate:yyyy-MM-dd}, active on {day:yyyy-MM-dd}: {agreement.IsActiveOn(day)}");

Console.WriteLine("--- JSON");
var dto = new DiscountAgreementDto(3, new DateOnly(2026, 3, 23), null);
Console.WriteLine(JsonSerializer.Serialize(dto, Web));

var omitNulls = new JsonSerializerOptions(Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
Console.WriteLine(JsonSerializer.Serialize(dto, omitNulls));

Console.WriteLine(JsonSerializer.Serialize(new { signedAt = DateTime.MinValue }, Web));

// A typed DTO cannot tell "leave it" from "clear it"
AgreementPatch? missing = JsonSerializer.Deserialize<AgreementPatch>("{}", Web);
AgreementPatch? explicitNull = JsonSerializer.Deserialize<AgreementPatch>("""{"endDate":null}""", Web);
Console.WriteLine($"missing: {missing!.EndDate?.ToString() ?? "null"}, explicit null: {explicitNull!.EndDate?.ToString() ?? "null"}");

Console.WriteLine("--- merge patch");
foreach (string body in new[] { "{}", """{"endDate":null}""", """{"endDate":"2026-12-31"}""" })
{
    var agreement = new DiscountAgreement { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 8, 31) };
    using JsonDocument patch = JsonDocument.Parse(body);
    AgreementPatching.ApplyPatch(agreement, patch.RootElement);
    Console.WriteLine($"{body,-26} -> {agreement.End}");
}
