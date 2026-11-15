namespace Bookstore.Privacy.Retention;

// Example periods from the inventory; the legal team owns the real values.
public static class RetentionPolicy
{
    public static readonly TimeSpan SignInSessions = TimeSpan.FromDays(180);

    // TTK 82: ten years, counted from the end of the calendar year of the record.
    public static DateTime OrderBillingRetainUntil(DateTime orderedAtUtc) =>
        new DateTime(orderedAtUtc.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddYears(10);
}
