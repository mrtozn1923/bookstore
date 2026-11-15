namespace Bookstore.Privacy.Consent;

public static class BusinessDays
{
    // Saturdays, Sundays and official holidays are not business days. Holidays come from configuration.
    public static DateOnly Add(DateOnly start, int days, IReadOnlySet<DateOnly> holidays)
    {
        DateOnly day = start;
        while (days > 0)
        {
            day = day.AddDays(1);
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(day))
            {
                days--;
            }
        }
        return day;
    }
}
