namespace Bookstore.Notifications.Tests;

public class InboxTests
{
    private static readonly DateTimeOffset Monday = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Marking_shown_notifications_as_read()
    {
        Inbox inbox = new();
        inbox.Add(Notification.Shipped(1, "Your order of Dune has shipped.", Monday));
        inbox.Add(Notification.Shipped(2, "Your order of Emma has shipped.", Monday));

        IReadOnlyList<Notification> shown = inbox.Unread(1);
        inbox.MarkAsRead(shown.Select(n => n.Id));

        Assert.Single(shown);
        Assert.Empty(inbox.Unread(1));
        Assert.Single(inbox.Unread(2));
    }

    [Fact]
    public void Every_added_notification_gets_an_id()
    {
        Inbox inbox = new();
        inbox.Add(Notification.Shipped(1, "Your order of Dune has shipped.", Monday));

        Assert.NotEqual(Guid.Empty, inbox.All[0].Id);
    }

    [Fact]
    public void Due_reminders_are_the_ones_in_the_past()
    {
        Inbox inbox = new();
        inbox.Add(Notification.Reminder(1, Monday));
        inbox.Add(Notification.Reminder(1, Monday.AddMinutes(1)));
        inbox.Add(Notification.Shipped(1, "Your order of Dune has shipped.", Monday));

        Assert.Single(inbox.DueReminders(now: Monday));
    }

    [Fact]
    public void Asking_for_unread_twice_changes_nothing()
    {
        Inbox inbox = new();
        inbox.Add(Notification.Shipped(1, "Your order of Dune has shipped.", Monday));

        Assert.Equal(inbox.Unread(1).Count, inbox.Unread(1).Count);
    }
}
