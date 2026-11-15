namespace Bookstore.Notifications;

public sealed class PriceDropAlert(Inbox inbox, IReadOnlyDictionary<int, IReadOnlyList<string>> wishlists,
    IReadOnlySet<int> marketingOptOut, string isbn, string title, decimal oldPrice, decimal newPrice)
{
    private decimal DropPercent => (oldPrice - newPrice) / oldPrice * 100;

    private string Text => FormattableString.Invariant($"{title} is now {newPrice:0.00} TRY ({DropPercent:0}% off).");

    // Back office preview: who would get the alert, without sending anything.
    public IReadOnlyList<int> Recipients() =>
        DropPercent < 10
            ? []
            : wishlists.Where(w => w.Value.Contains(isbn) && !marketingOptOut.Contains(w.Key))
                .Select(w => w.Key)
                .Order()
                .ToList();

    public int Execute(DateTimeOffset now)
    {
        IReadOnlyList<int> recipients = Recipients();
        foreach (int customerId in recipients)
            inbox.Add(Notification.PriceDrop(customerId, Text, now));
        return recipients.Count;
    }
}
