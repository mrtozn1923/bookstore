namespace Bookstore.Institutional.Domain;

// State machine'de olmayan bir geçiş istendi (API: 409)
public sealed class InvalidOrderTransitionException(OrderStatus status, string trigger)
    : InvalidOperationException($"Cannot {trigger} an order in status {status}.")
{
    public OrderStatus Status { get; } = status;
}

// Requirement'taki bir iş kuralı ihlal edildi (API: 422)
public sealed class OrderRuleException(string rule, string message) : Exception(message)
{
    public string Rule { get; } = rule;
}
