using Bookstore.Ordering;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

List<Order> orders = [new(1001, "ada@example.com", 605.50m, "Shipped")];

app.MapGet("/orders", () => orders);

app.MapGet("/orders/{id:int}", (int id) =>
    orders.FirstOrDefault(o => o.Id == id) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

app.Run();
