using System.Collections.Concurrent;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
WebApplication app = builder.Build();

ConcurrentDictionary<int, Order> orders = new();
Dictionary<int, decimal> listPrices = new() { [1] = 420m, [3] = 450m };   // Dune, The Hobbit
Dictionary<int, decimal> remainingBudgets = new() { [7] = 75_000m };       // Science, 2026 classroom-sets
int nextId = 41;

RouteGroupBuilder orderApi = app.MapGroup("/orders");

// UC-01 Sipariş Ver, adım 1-2: kullanıcı formu doldurur, sistem taslağı kaydeder
orderApi.MapPost("/", (CreateOrder body) =>
{
    Order order = new(Interlocked.Increment(ref nextId), body.PlacedById, body.InstitutionUnitId,
        body.BudgetCategoryCode, body.BookId, body.Quantity, body.DeliveryDate);
    orders[order.Id] = order;
    return TypedResults.Created($"/orders/{order.Id}", order);
});

// UC-01 adım 3-7: gönder; adım 5 <<include>> Bütçeyi Kontrol Et; uzantı 5a bütçe yetersiz
orderApi.MapPost("/{id:int}/submit", IResult (int id) =>
{
    if (!orders.TryGetValue(id, out Order? order))
        return TypedResults.NotFound();

    decimal total = Pricing.Total(listPrices[order.BookId], order.Quantity, discountPercent: 10);
    decimal remaining = remainingBudgets.GetValueOrDefault(order.InstitutionUnitId);
    if (order.BudgetCategoryCode != "donation" && remaining < total)
        return TypedResults.Problem(
            title: "Insufficient budget",
            detail: $"Order total is {total} TRY, remaining budget is {remaining} TRY.",
            statusCode: StatusCodes.Status409Conflict);

    Order submitted = order with { Total = total, Status = OrderStatus.PendingApprover };
    orders[id] = submitted;
    return TypedResults.Ok(submitted);
});

app.Run();

public sealed record CreateOrder(int PlacedById, int InstitutionUnitId, string BudgetCategoryCode, int BookId,
    int Quantity, DateOnly DeliveryDate);

public sealed record Order(int Id, int PlacedById, int InstitutionUnitId, string BudgetCategoryCode, int BookId,
    int Quantity, DateOnly DeliveryDate)
{
    public decimal Total { get; init; }
    public OrderStatus Status { get; init; } = OrderStatus.Draft;
}

public enum OrderStatus { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled }

public static class Pricing
{
    public static decimal Total(decimal listPrice, int quantity, int discountPercent)
    {
        decimal listTotal = listPrice * quantity;
        return listTotal - listTotal * discountPercent / 100;
    }
}
