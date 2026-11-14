using System.ComponentModel.DataAnnotations;

namespace Bookstore.MongoLab.EfCore;

// EF Core entities are mutable classes: change tracking compares property values, records with init cannot be edited in place
public sealed class Book
{
    public int Id { get; set; }
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public List<AuthorRef> Authors { get; set; } = [];
    public CategoryRef? Category { get; set; }
    public string Language { get; set; } = "";
    public int? PublishedYear { get; set; }
    public int? PageCount { get; set; }
    public decimal ListPrice { get; set; }
    public DateTime ListedOn { get; set; }
    public bool IsActive { get; set; }
    public Stock? Stock { get; set; }
    public List<Review> Reviews { get; set; } = [];

    // Row version: EF Core adds it to the update filter and increments it on every save
    [Timestamp]
    public long Version { get; set; }
}

public sealed class AuthorRef
{
    public int AuthorId { get; set; }
    public string Name { get; set; } = "";
}

public sealed class CategoryRef
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
}

public sealed class Stock
{
    public int OnHand { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class Review
{
    public MongoDB.Bson.ObjectId Id { get; set; }
    public int BookId { get; set; }
    public CustomerRef Customer { get; set; } = null!;
    public int Rating { get; set; }
    public string? Body { get; set; }
    public DateTime ReviewedAt { get; set; }
}

public sealed class CustomerRef
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = "";
}

public sealed class Order
{
    public int Id { get; set; }
    public CustomerRef Customer { get; set; } = null!;
    public DateTime OrderedAt { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime? ShippedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public string Currency { get; set; } = "TRY";
    public List<Payment> Payments { get; set; } = [];
}

public sealed class OrderItem
{
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class Payment
{
    public string Method { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
}
