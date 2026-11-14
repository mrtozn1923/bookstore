namespace Bookstore.Institutional.Application;

public sealed record CreateOrderCommand(int PlacedById, string BudgetCategoryCode, int BookId, int Quantity,
    DateOnly DeliveryDate, string? Note, bool Submit);

// İş kuralı ihlali: API bunu 409 Conflict'e çevirir
public sealed class OrderRuleException(string message) : Exception(message);

public sealed record DecideOrderCommand(int OrderId, int ApproverId, string? Comment);

public sealed record OrderResult(int Id, string Status, decimal Total);
