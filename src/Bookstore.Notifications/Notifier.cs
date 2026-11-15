using Bookstore.Ordering;

namespace Bookstore.Notifications;

public interface ISmsGateway
{
    void Send(string phone, string text);
}

public enum SmsResult
{
    Sent,
    Skipped
}

public class Notifier(Inbox inbox, ISmsGateway sms, IReadOnlyDictionary<int, string> phones, IReadOnlySet<int> smsOptOut)
{
    // Returns 0 when done, -1 for an order without items, -2 when no SMS could be sent.
    [Obsolete("Use NotifyShipped, NotifyShippedBySms or NotifyShippedBySmsUrgently. SendNotification will be removed in 2.0.")]
    public int SendNotification(Order order, bool viaSms, bool urgent)
    {
        if (order.Items.Count == 0)
            return -1;
        if (!viaSms)
        {
            NotifyShipped(order);
            return 0;
        }
        SmsResult result = urgent ? NotifyShippedBySmsUrgently(order) : NotifyShippedBySms(order);
        return result == SmsResult.Sent ? 0 : -2;
    }

    public void NotifyShipped(Order order) => PostShipped(order);

    public SmsResult NotifyShippedBySms(Order order) => TextShipped(order, PostShipped(order));

    public SmsResult NotifyShippedBySmsUrgently(Order order) => TextShipped(order, "URGENT: " + PostShipped(order));

    private string PostShipped(Order order)
    {
        if (order.Items.Count == 0)
            throw new ArgumentException("A shipped order must have at least one item.", nameof(order));
        string text = ShippedText(order);
        inbox.Add(Notification.Shipped(order.CustomerId, text, DateTimeOffset.UtcNow));
        return text;
    }

    private SmsResult TextShipped(Order order, string text)
    {
        if (smsOptOut.Contains(order.CustomerId) || !phones.TryGetValue(order.CustomerId, out string? phone))
            return SmsResult.Skipped;
        sms.Send(phone, text);
        return SmsResult.Sent;
    }

    private static string ShippedText(Order order) =>
        order.Items.Count == 1
            ? $"Your order of {order.Items[0].Title} has shipped."
            : $"Your order of {order.Items.Count} books has shipped.";
}
