using System.Globalization;
using System.Text.Json;
using Bookstore.Bcl.Dates;
using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Bcl.Samples.Dates;

public static class DateDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("date-guess", Guess),
        ("date-zones", Zones),
        ("date-convert", Convert),
        ("date-dst", Dst),
        ("date-parse", Parse),
        ("date-kind", Kind),
        ("date-iso", Iso),
        ("date-dateonly", DateOnlyDemo),
        ("date-month-end", MonthEnd),
        ("date-age", Age),
        ("date-timespan", TimeSpanDemo),
        ("date-delivery", Delivery),
    ];

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static void Guess()
    {
        DateTime approved = new(2027, 1, 31);
        Console.WriteLine(approved.AddMonths(1).ToString("yyyy-MM-dd", Invariant));
        Console.WriteLine(approved.AddMonths(1).AddMonths(1).ToString("yyyy-MM-dd", Invariant));
        Console.WriteLine(approved.AddMonths(2).ToString("yyyy-MM-dd", Invariant));

        TimeZoneInfo warehouse = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        Console.WriteLine(warehouse.Id);
    }

    private static void Zones()
    {
        foreach (string id in new[] { "Turkey Standard Time", "Europe/Istanbul", "W. Europe Standard Time", "Europe/Berlin" })
        {
            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            Console.WriteLine($"{id,-24} -> Id={zone.Id,-24} HasIanaId={zone.HasIanaId,-5} BaseUtcOffset={zone.BaseUtcOffset}");
        }

        TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Istanbul", out string? windowsId);
        Console.WriteLine($"Europe/Istanbul -> Windows: {windowsId}");
        TimeZoneInfo.TryConvertWindowsIdToIanaId("W. Europe Standard Time", out string? ianaId);
        Console.WriteLine($"W. Europe Standard Time -> IANA: {ianaId}");
        TimeZoneInfo.TryConvertWindowsIdToIanaId("W. Europe Standard Time", "NL", out string? ianaNl);
        Console.WriteLine($"W. Europe Standard Time + NL -> IANA: {ianaNl}");
        bool known = TimeZoneInfo.TryConvertIanaIdToWindowsId("Europe/Frankfurt", out string? missing);
        Console.WriteLine($"Europe/Frankfurt known: {known} ({missing ?? "null"})");

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Frankfurt");
        }
        catch (TimeZoneNotFoundException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Convert()
    {
        TimeZoneInfo istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        TimeZoneInfo warehouse = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        // Corporate orders are approved in Istanbul time; the warehouse is in Frankfurt.
        DateTimeOffset[] approvals =
        [
            new(2026, 10, 23, 17, 30, 0, TimeSpan.FromHours(3)),
            new(2026, 10, 26, 17, 30, 0, TimeSpan.FromHours(3)),
        ];

        foreach (DateTimeOffset approvedAt in approvals)
        {
            DateTimeOffset atWarehouse = TimeZoneInfo.ConvertTime(approvedAt, warehouse);
            Console.WriteLine($"Istanbul {approvedAt:yyyy-MM-dd HH:mm zzz} -> warehouse {atWarehouse:yyyy-MM-dd HH:mm zzz}  (UTC {approvedAt.UtcDateTime:HH:mm})");
        }

        DateTime pickupLocal = new(2026, 11, 16, 15, 0, 0, DateTimeKind.Unspecified);
        DateTime pickupUtc = TimeZoneInfo.ConvertTimeToUtc(pickupLocal, warehouse);
        DateTime pickupIstanbul = TimeZoneInfo.ConvertTime(pickupLocal, warehouse, istanbul);
        Console.WriteLine($"Courier pickup 15:00 Berlin = {pickupUtc:HH:mm} UTC = {pickupIstanbul:HH:mm} Istanbul");

        DateTimeOffset byId = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(approvals[0], "W. Europe Standard Time");
        Console.WriteLine($"ConvertTimeBySystemTimeZoneId: {byId:yyyy-MM-dd HH:mm zzz}");
    }

    private static void Dst()
    {
        TimeZoneInfo warehouse = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        DateTime springGap = new(2026, 3, 29, 2, 30, 0);
        DateTime autumnOverlap = new(2026, 10, 25, 2, 30, 0);

        Console.WriteLine($"{springGap:yyyy-MM-dd HH:mm} invalid: {warehouse.IsInvalidTime(springGap)}");
        Console.WriteLine($"{autumnOverlap:yyyy-MM-dd HH:mm} ambiguous: {warehouse.IsAmbiguousTime(autumnOverlap)}, offsets: {string.Join(" / ", warehouse.GetAmbiguousTimeOffsets(autumnOverlap))}");

        try
        {
            TimeZoneInfo.ConvertTimeToUtc(springGap, warehouse);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message.Split(" (Parameter")[0]}");
        }

        DateTime utc = TimeZoneInfo.ConvertTimeToUtc(autumnOverlap, warehouse);
        Console.WriteLine($"ConvertTimeToUtc(ambiguous) = {utc:HH:mm} UTC (standard offset chosen)");

        DateTimeOffset start = new(2026, 10, 24, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset nextDayWall = new(2026, 10, 25, 12, 0, 0, TimeSpan.FromHours(1));
        Console.WriteLine($"Saturday 12:00 -> Sunday 12:00 Berlin = {(nextDayWall - start).TotalHours} hours");
    }

    private static void Parse()
    {
        string[] formats = ["dd.MM.yyyy HH:mm", "dd.MM.yyyy", "yyyy-MM-dd'T'HH:mm"];
        string[] inputs = ["14.11.2026 09:30", "14.11.2026", "2026-11-14T09:30", "14/11/2026", "4.11.2026"];

        foreach (string input in inputs)
        {
            bool ok = DateTime.TryParseExact(input, formats, Invariant, DateTimeStyles.None, out DateTime value);
            Console.WriteLine($"{input,-18} -> {(ok ? value.ToString("yyyy-MM-dd HH:mm", Invariant) : "rejected")}");
        }

        try
        {
            DateTime.ParseExact("2026-11-14 9:30", "yyyy-MM-dd HH:mm", Invariant);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"FormatException: {ex.Message}");
        }

        DateTime single = DateTime.ParseExact("2026-11-14 9:30", "yyyy-MM-dd H:mm", Invariant);
        Console.WriteLine($"'H:mm' accepts 9:30 -> {single:HH:mm}");
    }

    private static void Kind()
    {
        // This demo ran in a container with TZ=Europe/Istanbul.
        string utcText = "2026-11-14T09:30:00Z";
        string noOffset = "2026-11-14T09:30:00";

        DateTime plain = DateTime.Parse(utcText, Invariant);
        DateTime adjusted = DateTime.Parse(utcText, Invariant, DateTimeStyles.AdjustToUniversal);
        DateTime roundtrip = DateTime.Parse(utcText, Invariant, DateTimeStyles.RoundtripKind);
        DateTime unspecified = DateTime.Parse(noOffset, Invariant, DateTimeStyles.RoundtripKind);
        DateTime assumed = DateTime.Parse(noOffset, Invariant, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        Console.WriteLine($"Parse(Z)                   -> {plain:HH:mm} Kind={plain.Kind}");
        Console.WriteLine($"Parse(Z, AdjustToUniversal)-> {adjusted:HH:mm} Kind={adjusted.Kind}");
        Console.WriteLine($"Parse(Z, RoundtripKind)    -> {roundtrip:HH:mm} Kind={roundtrip.Kind}");
        Console.WriteLine($"Parse(no offset)           -> {unspecified:HH:mm} Kind={unspecified.Kind}");
        Console.WriteLine($"  .ToUniversalTime()       -> {unspecified.ToUniversalTime():HH:mm} (treated as local)");
        Console.WriteLine($"Parse(no offset, AssumeUniversal|AdjustToUniversal) -> {assumed:HH:mm} Kind={assumed.Kind}");

        DateTimeOffset offset = DateTimeOffset.Parse(noOffset, Invariant);
        Console.WriteLine($"DateTimeOffset.Parse(no offset) -> {offset:yyyy-MM-dd HH:mm zzz}");
    }

    private static void Iso()
    {
        DateTimeOffset approvedAt = new(2026, 11, 14, 9, 30, 15, 250, TimeSpan.FromHours(3));
        string wire = approvedAt.ToString("o", Invariant);
        DateTimeOffset back = DateTimeOffset.ParseExact(wire, "o", Invariant);
        Console.WriteLine($"DateTimeOffset \"o\" : {wire}");
        Console.WriteLine($"Round-trip equal   : {back == approvedAt}, EqualsExact: {back.EqualsExact(approvedAt)}");
        Console.WriteLine($"Same instant in UTC: {approvedAt.ToUniversalTime():o}");
        Console.WriteLine($"== UTC version     : {approvedAt == approvedAt.ToUniversalTime()}, EqualsExact: {approvedAt.EqualsExact(approvedAt.ToUniversalTime())}");

        DateTime utc = new(2026, 11, 14, 6, 30, 15, DateTimeKind.Utc);
        DateTime unspecified = new(2026, 11, 14, 6, 30, 15);
        Console.WriteLine($"DateTime Utc \"o\"   : {utc:o}");
        Console.WriteLine($"Unspecified \"o\"    : {unspecified:o}");
        Console.WriteLine($"\"s\" / \"u\"          : {utc:s} / {utc:u}");
        Console.WriteLine($"JSON DateTimeOffset: {JsonSerializer.Serialize(approvedAt)}");
        Console.WriteLine($"JSON Unspecified   : {JsonSerializer.Serialize(unspecified)}");
    }

    private static void DateOnlyDemo()
    {
        DateOnly delivery = new(2026, 11, 20);
        TimeOnly slot = new(14, 30);
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");

        Console.WriteLine($"ToString(tr-TR)     : {delivery.ToString(tr)} {slot.ToString(tr)}");
        Console.WriteLine($"ToString(\"O\")       : {delivery:O} {slot:O}");
        Console.WriteLine($"ParseExact dd.MM.yyyy: {DateOnly.ParseExact("20.11.2026", "dd.MM.yyyy", Invariant):O}");
        Console.WriteLine($"TryParse with time  : {DateOnly.TryParse("2026-11-20 14:30", Invariant, out _)}");

        string json = JsonSerializer.Serialize(new DeliverySlot(delivery, slot));
        Console.WriteLine($"JSON                : {json}");
        Console.WriteLine($"Back                : {JsonSerializer.Deserialize<DeliverySlot>(json)}");

        try
        {
            JsonSerializer.Deserialize<DeliverySlot>("""{"Day":"20.11.2026","At":"14:30"}""");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JsonException       : {ex.Message.Split(" Path:")[0]}");
        }

        DateOnly fromDateTime = DateOnly.FromDateTime(new DateTime(2026, 11, 20, 23, 59, 0));
        Console.WriteLine($"FromDateTime        : {fromDateTime:O}, ToDateTime: {delivery.ToDateTime(slot):O}");
        Console.WriteLine($"ISO week (.NET 10)  : {ISOWeek.GetWeekOfYear(delivery)}");
    }

    private static void MonthEnd()
    {
        DateTime invoiceDay = new(2027, 1, 31);
        for (int i = 1; i <= 3; i++)
            Console.Write($"AddMonths({i}) = {invoiceDay.AddMonths(i):yyyy-MM-dd}   ");
        Console.WriteLine();

        DateTime chained = invoiceDay;
        for (int i = 1; i <= 3; i++)
        {
            chained = chained.AddMonths(1);
            Console.Write($"chained {i} = {chained:yyyy-MM-dd}   ");
        }
        Console.WriteLine();

        DateOnly start = new(2027, 1, 31);
        for (int i = 1; i <= 3; i++)
            Console.Write($"EndOfMonth(+{i}) = {BusinessCalendar.EndOfMonth(start.AddMonths(i)):yyyy-MM-dd}   ");
        Console.WriteLine();

        Console.WriteLine($"2028-02-29 AddYears(1) = {new DateOnly(2028, 2, 29).AddYears(1):yyyy-MM-dd}");
        Console.WriteLine($"Days between 2026-11-14 and 2027-01-01 = {new DateOnly(2027, 1, 1).DayNumber - new DateOnly(2026, 11, 14).DayNumber}");
    }

    private static void Age()
    {
        static int AgeOn(DateOnly birthDate, DateOnly today)
        {
            int age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age))
                age--;
            return age;
        }

        DateOnly ada = new(2008, 11, 15);
        DateOnly leapling = new(2008, 2, 29);
        Console.WriteLine($"Ada on 2026-11-14     : {AgeOn(ada, new DateOnly(2026, 11, 14))}");
        Console.WriteLine($"Ada on 2026-11-15     : {AgeOn(ada, new DateOnly(2026, 11, 15))}");
        Console.WriteLine($"29 Feb on 2026-02-28  : {AgeOn(leapling, new DateOnly(2026, 2, 28))}");
        Console.WriteLine($"29 Feb on 2026-03-01  : {AgeOn(leapling, new DateOnly(2026, 3, 1))}");
        Console.WriteLine($"Naive days/365.25 on 2026-11-15: {(int)((new DateOnly(2026, 11, 15).DayNumber - ada.DayNumber) / 365.25)}");
    }

    private static void TimeSpanDemo()
    {
        TimeSpan transit = TimeSpan.FromHours(26) + TimeSpan.FromMinutes(5);
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");

        Console.WriteLine($"ToString()         : {transit}");
        Console.WriteLine($"\"c\" / \"g\" (tr-TR)  : {transit.ToString("c")} / {transit.ToString("g", tr)}");
        Console.WriteLine($"@\"hh\\:mm\"          : {transit.ToString(@"hh\:mm")}");
        Console.WriteLine($"@\"d\\.hh\\:mm\"       : {transit.ToString(@"d\.hh\:mm")}");
        Console.WriteLine($"TotalHours based   : {(int)transit.TotalHours}:{transit.Minutes:00}");
        Console.WriteLine($"Hours / TotalHours : {transit.Hours} / {transit.TotalHours.ToString(Invariant)}");
        Console.WriteLine($"Parse(\"1.02:05\")   : {TimeSpan.Parse("1.02:05", Invariant).TotalMinutes} minutes");
        Console.WriteLine($"FromMilliseconds   : {TimeSpan.FromMilliseconds(1500)}");
    }

    private static void Delivery()
    {
        TimeZoneInfo istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        BusinessCalendar calendar = new([new DateOnly(2026, 10, 29)]);

        // In a test we decide what "now" is: Thursday 22 October 2026, 10:00 in Istanbul.
        FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 22, 7, 0, 0, TimeSpan.Zero));
        DeliveryDateCalculator calculator = new(clock, calendar, istanbul);

        Print(calculator.QuoteForApprovalNow());
        clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(10));
        Print(calculator.QuoteForApprovalNow());
        clock.SetUtcNow(new DateTimeOffset(2026, 10, 26, 6, 0, 0, TimeSpan.Zero));
        Print(calculator.QuoteForApprovalNow());

        static void Print(DeliveryQuote quote) =>
            Console.WriteLine($"approved {quote.ApprovedAt:ddd yyyy-MM-dd HH:mm zzz} after cut-off={quote.AfterCutOff,-5} -> delivery {quote.DeliveryDate:ddd yyyy-MM-dd}");
    }

    private sealed record DeliverySlot(DateOnly Day, TimeOnly At);
}
