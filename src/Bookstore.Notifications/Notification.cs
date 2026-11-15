namespace Bookstore.Notifications;

public class Notification
{
    private Notification(int customerId, string kind, string text, DateTimeOffset sendAt)
    {
        CustomerId = customerId;
        Kind = kind;
        Text = text;
        SendAt = sendAt;
    }

    public static Notification Shipped(int customerId, string text, DateTimeOffset at) =>
        new(customerId, "shipped", text, at);

    public static Notification Reminder(int customerId, DateTimeOffset sendAt) =>
        new(customerId, "reminder", "Your cart is waiting for you.", sendAt);

    public static Notification PriceDrop(int customerId, string text, DateTimeOffset at) =>
        new(customerId, "price-drop", text, at);

    public Guid Id { get; } = Guid.NewGuid();
    public int CustomerId { get; }
    public string Kind { get; }
    public string Text { get; }
    public DateTimeOffset SendAt { get; }
    public bool IsRead { get; set; }
}
