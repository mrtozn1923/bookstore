using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

// A slimmed-down Bookstore.Api for the formats series: in-memory data, no database
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddOptions<CatalogOptions>()
    .Bind(builder.Configuration.GetSection("Catalog"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

WebApplication app = builder.Build();
app.MapOpenApi();

Book[] books =
[
    new("9780441013593", "Dune", "Frank Herbert", 24.90m, 12),
    new("9780141439587", "Emma", "Jane Austen", 14.50m, 0),
    new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 19.90m, 7),
    new("9780141439549", "Middlemarch", "George Eliot", 17.25m, 3),
];

Order[] orders =
[
    new("BS-1001", "Ada", "Paid", [new("9780441013593", 2, 24.90m)]),
    new("BS-1002", "Alan", "Shipped", [new("9780547928227", 1, 19.90m), new("9780141439549", 1, 17.25m)]),
    new("BS-1003", "Grace", "AwaitingApproval", [new("9780441013593", 25, 22.41m)]),
];

app.MapGet("/api/books", (IOptions<CatalogOptions> options) =>
    books.Take(options.Value.PageSize));
app.MapGet("/api/orders", () => orders);

app.Run();

record Book(string Isbn, string Title, string Author, decimal Price, int Stock);
record OrderItem(string Isbn, int Quantity, decimal UnitPrice);
record Order(string OrderNumber, string CustomerName, string Status, OrderItem[] Items);

class CatalogOptions
{
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [Range(1, 500)]
    public int MaxPageSize { get; set; } = 100;
}
