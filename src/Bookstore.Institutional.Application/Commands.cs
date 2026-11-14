using Bookstore.Institutional.Domain;

namespace Bookstore.Institutional.Application;

public sealed record CreateOrderCommand(int PlacedById, string BudgetCategoryCode, int BookId, int Quantity,
    DateOnly DeliveryDate, string? Note, bool Submit,
    ShipmentPreference ShipmentPreference = ShipmentPreference.SingleShipment, int? MinFirstShipment = null);

// Kayıt yok: API bunu 404'e çevirir
public sealed class OrderNotFoundException(string message) : Exception(message);

public sealed record DecideOrderCommand(int OrderId, int ApproverId, string? Comment);

public sealed record OrderResult(int Id, string Status, decimal Total, string ShipmentPreference, int? MinFirstShipment);

public sealed record UnitBudgetResult(string BudgetCategory, int Year, decimal AllocatedAmount, decimal SpentAmount,
    decimal RemainingAmount);
