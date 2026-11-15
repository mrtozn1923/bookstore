namespace Bookstore.Notifications;

public class ReminderScheduler(Inbox inbox)
{
    public void RemindAfter(int days, int customerId, DateTimeOffset cartUpdatedAt) =>
        inbox.Add(Notification.Reminder(customerId, QuietHoursAdjusted(cartUpdatedAt.AddDays(days))));

    private static bool IsNight(DateTimeOffset at) => at.Hour >= 22 || at.Hour < 8;

    // Night-time reminders wait until 08:00.
    private static DateTimeOffset QuietHoursAdjusted(DateTimeOffset at)
    {
        if (!IsNight(at))
            return at;
        DateTimeOffset day = at.Hour >= 22 ? at.AddDays(1) : at;
        return new DateTimeOffset(day.Year, day.Month, day.Day, 8, 0, 0, day.Offset);
    }
}
