namespace Bookstore.Prompts.Tasks.OrderAssistant;

public enum OrderStatus { Pending, Shipped, Delivered, Cancelled }

public sealed record StoreOrder(string Number, string CustomerId, OrderStatus Status, string Address, IReadOnlyList<string> Books);

// Authorization lives here, not in the prompt: every operation checks the owner and the order state
public sealed class OrderStore(IEnumerable<StoreOrder> orders)
{
    readonly Dictionary<string, StoreOrder> orders = orders.ToDictionary(o => o.Number);

    public List<string> Changes { get; } = [];

    public static OrderStore Seed() => new(
    [
        new("10428", "c-ada", OrderStatus.Pending, "Moda Cad. 3, Kadikoy, Istanbul", ["Dune", "The Hobbit"]),
        new("10431", "c-ada", OrderStatus.Shipped, "Moda Cad. 3, Kadikoy, Istanbul", ["Emma"]),
        new("10402", "c-ada", OrderStatus.Delivered, "Moda Cad. 3, Kadikoy, Istanbul", ["Middlemarch"]),
        new("20117", "c-alan", OrderStatus.Pending, "Ataturk Bulvari 8, Cankaya, Ankara", ["Dune"]),
    ]);

    StoreOrder? Find(string customerId, string number) =>
        orders.TryGetValue(number.Trim().TrimStart('#'), out StoreOrder? order) && order.CustomerId == customerId ? order : null;

    public string Describe(string customerId, string number) =>
        Find(customerId, number) is { } o
            ? $"Order {o.Number}: status {o.Status}, books {string.Join(", ", o.Books)}, shipping address {o.Address}."
            : $"Order {number} was not found for this customer.";

    public string ChangeAddress(string customerId, string number, string address)
    {
        if (Find(customerId, number) is not { } order)
        {
            return $"Order {number} was not found for this customer.";
        }
        if (order.Status != OrderStatus.Pending)
        {
            return $"Order {number} is {order.Status}; the address can only be changed before dispatch.";
        }
        orders[order.Number] = order with { Address = address };
        Changes.Add($"address {order.Number}");
        return $"The shipping address of order {order.Number} is now {address}.";
    }

    public string Cancel(string customerId, string number)
    {
        if (Find(customerId, number) is not { } order)
        {
            return $"Order {number} was not found for this customer.";
        }
        if (order.Status != OrderStatus.Pending)
        {
            return $"Order {number} is {order.Status}; only orders that have not been shipped can be cancelled.";
        }
        orders[order.Number] = order with { Status = OrderStatus.Cancelled };
        Changes.Add($"cancel {order.Number}");
        return $"Order {order.Number} is cancelled.";
    }

    public string RequestReturn(string customerId, string number, string reason)
    {
        if (Find(customerId, number) is not { } order)
        {
            return $"Order {number} was not found for this customer.";
        }
        if (order.Status != OrderStatus.Delivered)
        {
            return $"Order {number} is {order.Status}; a return request needs a delivered order.";
        }
        Changes.Add($"return-request {order.Number}");
        return $"Return request for order {order.Number} is created and waits for the returns team. Nothing is approved or refunded yet.";
    }

    public string HandOff(string summary)
    {
        Changes.Add("hand-off");
        return "A customer service person will take over this conversation.";
    }
}
