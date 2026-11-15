namespace Bookstore.Notifications.Tests;

public class ReminderTests
{
    [Fact]
    public Task Reminder_times() =>
        Combination()
            .Verify(
                (string cartUpdatedAt, int days) =>
                {
                    Inbox inbox = new();
                    ReminderScheduler scheduler = new(inbox);
                    DateTimeOffset at = DateTimeOffset.Parse(cartUpdatedAt, System.Globalization.CultureInfo.InvariantCulture);
                    scheduler.RemindAfter(days, 7, at);
                    return inbox.All.Select(n => $"{n.CustomerId} {n.Kind} {n.SendAt:yyyy-MM-dd HH:mm} {n.Text}").Single();
                },
                ["2026-11-16T10:00:00+00:00", "2026-11-16T22:30:00+00:00", "2026-11-16T06:15:00+00:00"],
                [1, 3]);

    [Fact]
    public Task Price_drop_alerts()
    {
        Inbox inbox = new();
        Dictionary<int, IReadOnlyList<string>> wishlists = new()
        {
            [1] = ["9780441013593", "9780141439587"],
            [2] = ["9780441013593"],
            [3] = ["9780141439587"]
        };
        DateTimeOffset now = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

        int small = new PriceDropAlert(inbox, wishlists, new HashSet<int>(), "9780441013593", "Dune", 120m, 115m).Execute(now);
        int big = new PriceDropAlert(inbox, wishlists, new HashSet<int> { 2 }, "9780441013593", "Dune", 120m, 90m).Execute(now);

        return Verify(new { small, big, Inbox = inbox.All.Select(n => $"{n.CustomerId} {n.Kind} {n.Text}") });
    }

    [Fact]
    public void Price_drop_preview_sends_nothing()
    {
        Inbox inbox = new();
        Dictionary<int, IReadOnlyList<string>> wishlists = new() { [1] = ["9780441013593"], [2] = ["9780441013593"] };
        PriceDropAlert alert = new(inbox, wishlists, new HashSet<int>(), "9780441013593", "Dune", 120m, 90m);

        Assert.Equal([1, 2], alert.Recipients());
        Assert.Empty(inbox.All);
    }
}
