namespace Bookstore.Ordering.Time;

public static class BusinessDay
{
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRange(DateOnly day, TimeZoneInfo zone)
        => (StartOfDay(day, zone), StartOfDay(day.AddDays(1), zone));

    private static DateTimeOffset StartOfDay(DateOnly day, TimeZoneInfo zone)
    {
        DateTime local = day.ToDateTime(TimeOnly.MinValue);

        // if midnight falls into a spring-forward gap, take the first valid minute of the day
        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        // if midnight happens twice when clocks go back, take the first one (larger offset = earlier instant)
        TimeSpan offset = zone.IsAmbiguousTime(local)
            ? zone.GetAmbiguousTimeOffsets(local).Max()
            : zone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}

public static class TimeProviderExtensions
{
    public static DateOnly TodayIn(this TimeProvider time, TimeZoneInfo zone)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), zone).DateTime);
}

public sealed class DeliveryDatePolicy(TimeProvider time)
{
    public bool IsAllowed(DateOnly requested, TimeZoneInfo institutionZone)
        => requested >= time.TodayIn(institutionZone);
}

public sealed record Appointment(string Title, DateTime LocalTime, string TimeZoneId)
{
    // the instant is computed on every read with the current zone rules
    public DateTimeOffset ToInstant()
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(LocalTime, zone), TimeSpan.Zero);
    }
}
