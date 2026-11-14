using System.Globalization;
using System.Text.Json;
using Bookstore.Ordering.Time;
using Microsoft.Extensions.Time.Testing;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
if (args.Contains("--zone"))
{
    Console.WriteLine($"Local zone: {TimeZoneInfo.Local.Id}");
    Console.WriteLine($"DateTime.Now kind: {DateTime.Now.Kind}");
    return;
}

// 1) The opening: yesterday's orders of an institution in Istanbul
List<OrderRow> orders =
[
    new(1, "Ada", DateTimeOffset.Parse("2026-11-05T21:10:00Z")),  // 00:10 on Nov 6 in Istanbul
    new(2, "Alan", DateTimeOffset.Parse("2026-11-06T09:45:00Z")), // 12:45 on Nov 6 in Istanbul
    new(3, "Grace", DateTimeOffset.Parse("2026-11-06T22:30:00Z")) // 01:30 on Nov 7 in Istanbul
];
DateOnly reportDay = new(2026, 11, 6);

string[] naive = orders
    .Where(o => DateOnly.FromDateTime(o.CreatedAt.UtcDateTime) == reportDay)
    .Select(o => o.PlacedBy).ToArray();
Console.WriteLine($"Naive (UTC date) {reportDay:yyyy-MM-dd}: {string.Join(", ", naive)}");

// 2) Day counts across a DST change
TimeZoneInfo berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
(DateTimeOffset d0, DateTimeOffset d1) = BusinessDay.GetUtcRange(new DateOnly(2026, 3, 29), berlin);
Console.WriteLine($"2026-03-29 in Berlin lasts {(d1 - d0).TotalHours} hours");
DateTimeOffset campaignStart = BusinessDay.GetUtcRange(new DateOnly(2026, 3, 27), berlin).StartUtc;
DateTimeOffset campaignEnd = BusinessDay.GetUtcRange(new DateOnly(2026, 3, 30), berlin).StartUtc;
int naiveDays = (int)((campaignEnd - campaignStart).TotalHours / 24);
int realDays = new DateOnly(2026, 3, 30).DayNumber - new DateOnly(2026, 3, 27).DayNumber;
Console.WriteLine($"Campaign Mar 27 -> Mar 30, naive: {naiveDays} days, DateOnly: {realDays} days");

// 3) The institution's day as a UTC range
TimeZoneInfo istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
(DateTimeOffset startUtc, DateTimeOffset endUtc) = BusinessDay.GetUtcRange(reportDay, istanbul);
string[] correct = orders
    .Where(o => o.CreatedAt >= startUtc && o.CreatedAt < endUtc)
    .Select(o => o.PlacedBy).ToArray();
Console.WriteLine($"Range: [{startUtc:O}, {endUtc:O})");
Console.WriteLine($"Institution day {reportDay:yyyy-MM-dd}: {string.Join(", ", correct)}");

TimeZoneInfo beirut = TimeZoneInfo.FindSystemTimeZoneById("Asia/Beirut");
Console.WriteLine($"Beirut 2026-03-29 00:00 invalid: {beirut.IsInvalidTime(new DateTime(2026, 3, 29, 0, 0, 0))}");
(DateTimeOffset bs, DateTimeOffset be) = BusinessDay.GetUtcRange(new DateOnly(2026, 3, 29), beirut);
Console.WriteLine($"Beirut 2026-03-29: [{bs:O}, {be:O})");

// 4) TimeProvider: "today" in the institution's zone
FakeTimeProvider clock = new(DateTimeOffset.Parse("2026-11-05T20:30:00Z"));
DeliveryDatePolicy policy = new(clock);
DateOnly requested = new(2026, 11, 5);
Console.WriteLine($"20:30Z  Istanbul today: {clock.TodayIn(istanbul):yyyy-MM-dd}, can request {requested:yyyy-MM-dd}: {policy.IsAllowed(requested, istanbul)}");
clock.Advance(TimeSpan.FromHours(2));
Console.WriteLine($"22:30Z  Istanbul today: {clock.TodayIn(istanbul):yyyy-MM-dd}, can request {requested:yyyy-MM-dd}: {policy.IsAllowed(requested, istanbul)}");
Console.WriteLine($"22:30Z  UTC today: {DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime):yyyy-MM-dd}");
try
{
    clock.SetUtcNow(DateTimeOffset.Parse("2026-11-05T20:30:00Z"));
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
}

// 5) A future appointment and a rule change
DateTime meetingLocal = new(2016, 11, 15, 10, 0, 0);
TimeSpan offsetAssumedIn2015 = TimeSpan.FromHours(2); // winter time (EET) under the old rules
DateTimeOffset storedUtc = new DateTimeOffset(meetingLocal, offsetAssumedIn2015).ToUniversalTime();
DateTimeOffset shownToday = TimeZoneInfo.ConvertTime(storedUtc, istanbul);
Console.WriteLine($"Stored in 2015 as UTC: {storedUtc:O}");
Console.WriteLine($"Shown with today's rules: {shownToday:yyyy-MM-dd HH:mm zzz}");
Console.WriteLine($"Istanbul offset 2015-11-15: {istanbul.GetUtcOffset(new DateTime(2015, 11, 15))}");
Console.WriteLine($"Istanbul offset 2016-11-15: {istanbul.GetUtcOffset(new DateTime(2016, 11, 15))}");

Appointment signing = new("Book signing with Ada", new DateTime(2027, 4, 12, 10, 0, 0), "Europe/Istanbul");
Console.WriteLine($"Appointment instant (resolved now): {signing.ToInstant():O}");

// 6) JSON: DateOnly as yyyy-MM-dd
CustomerDto dto = new("Ada", new DateOnly(1990, 5, 14), DateTimeOffset.Parse("2026-11-05T22:30:00Z"));
Console.WriteLine(JsonSerializer.Serialize(dto, JsonSerializerOptions.Web));
Console.WriteLine(TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Istanbul", out string? windowsId) ? windowsId : "none");

public record OrderRow(int Id, string PlacedBy, DateTimeOffset CreatedAt);

public record CustomerDto(string Name, DateOnly BirthDate, DateTimeOffset CreatedAt);
