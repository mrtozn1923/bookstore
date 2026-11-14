using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bookstore.MongoLab;

public sealed record Book
{
    public int Id { get; init; }
    public required string Isbn { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public List<AuthorRef> Authors { get; init; } = [];
    public CategoryRef? Category { get; init; }
    public required string Language { get; init; }
    public int? PublishedYear { get; init; }
    public int? PageCount { get; init; }
    public decimal ListPrice { get; init; }
    public DateTime ListedOn { get; init; }
    public bool IsActive { get; init; }
    public Stock? Stock { get; init; }
}

public sealed record AuthorRef(int AuthorId, string Name);
public sealed record CategoryRef(int CategoryId, string Name);
public sealed record Stock(int OnHand, int ReorderLevel, DateTime UpdatedAt);

public enum OrderStatus { Pending, Paid, Shipped, Delivered, Cancelled }

public sealed record Order
{
    public int Id { get; init; }
    public required CustomerRef Customer { get; init; }
    public DateTime OrderedAt { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime? ShippedAt { get; init; }
    public List<OrderItem> Items { get; init; } = [];
    public decimal Total { get; init; }
    public required string Currency { get; init; }
    public List<Payment> Payments { get; init; } = [];
}

public sealed record CustomerRef(int CustomerId, string Name);
public sealed record OrderItem(int BookId, string Title, int Quantity, decimal UnitPrice);
public sealed record Payment(string Method, decimal Amount, DateTime PaidAt);

public sealed record Review
{
    public ObjectId Id { get; init; }
    public int BookId { get; init; }
    public required CustomerRef Customer { get; init; }
    public int Rating { get; init; }
    public string? Body { get; init; }
    public DateTime ReviewedAt { get; init; }
}

// A Guid key: the driver needs to know how to store it
public sealed record StockReservation(Guid Id, int BookId, int OrderId, int Quantity, DateTime ExpiresAt);

// Polymorphic editions (post 5): one collection, the _t field tells the types apart
[BsonKnownTypes(typeof(PrintEdition), typeof(Ebook), typeof(Audiobook))]
public abstract record Edition
{
    public required string Id { get; init; }
    public int BookId { get; init; }
    public decimal Price { get; init; }
}

public sealed record PrintEdition : Edition
{
    public required string Binding { get; init; }
    public int PageCount { get; init; }
}

public sealed record Ebook : Edition
{
    public List<string> FileFormats { get; init; } = [];
    public bool Drm { get; init; }
}

public sealed record Audiobook : Edition
{
    public required string Narrator { get; init; }
    public int DurationMinutes { get; init; }
}

// Which BSON type does each .NET type become?
public sealed record TypeSample(int Id, decimal Price, DateTime CreatedAt, DateTimeOffset LocalTime, Guid Token, OrderStatus Status);
