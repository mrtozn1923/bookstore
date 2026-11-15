namespace Bookstore.Notifications;

public class Inbox
{
    private readonly List<Notification> _items = [];

    public IReadOnlyList<Notification> All => _items.AsReadOnly();

    public void Add(Notification notification) => _items.Add(notification);

    public IReadOnlyList<Notification> Unread(int customerId) =>
        _items.Where(n => n.CustomerId == customerId && !n.IsRead).ToList();

    public void MarkAsRead(IEnumerable<Guid> ids)
    {
        HashSet<Guid> toMark = [.. ids];
        foreach (Notification notification in _items.Where(n => toMark.Contains(n.Id)))
            notification.IsRead = true;
    }

    public IReadOnlyList<Notification> DueReminders(DateTimeOffset now) =>
        _items.Where(n => n.Kind == "reminder" && n.SendAt <= now).ToList();
}
