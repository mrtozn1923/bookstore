namespace Bookstore.Bcl.Dates;

// Working days for Bookstore's corporate orders: Monday to Friday, minus public holidays.
public sealed class BusinessCalendar(IEnumerable<DateOnly> holidays)
{
    private readonly HashSet<DateOnly> _holidays = [.. holidays];

    public bool IsBusinessDay(DateOnly day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_holidays.Contains(day);

    public DateOnly AddBusinessDays(DateOnly start, int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days);

        DateOnly day = start;
        while (days > 0)
        {
            day = day.AddDays(1);
            if (IsBusinessDay(day))
                days--;
        }
        return day;
    }

    public static DateOnly EndOfMonth(DateOnly day) =>
        new(day.Year, day.Month, DateTime.DaysInMonth(day.Year, day.Month));
}
