namespace Bookstore.Ordering;

public static class SalesReport
{
    public static decimal Revenue(IEnumerable<Order> orders, DateRange period) =>
        orders.Where(o => period.Contains(o.PlacedOn)).Sum(o => o.Total);

    public static int OrderCount(IEnumerable<Order> orders, DateRange period) =>
        orders.Count(o => period.Contains(o.PlacedOn));

    public static int OrdersOfCustomer(IEnumerable<Order> orders, int customerId) =>
        orders.Count(o => o.CustomerId == customerId);
}

public static class LoyaltyReport
{
    public static string Line(Customer customer) =>
        $"{customer.Name} ({customer.Id}): {customer.LoyaltyPoints} points, {customer.Address.City}";
}

public static class ShippingQuote
{
    private static readonly string[] MetroCities = ["Istanbul", "Ankara", "Izmir"];

    public static decimal FeeFor(Order order)
    {
        if (order.Total >= 300m)
            return 0m;
        return MetroCities.Contains(order.ShippingAddress.City) ? 49.90m : 64.90m;
    }
}
