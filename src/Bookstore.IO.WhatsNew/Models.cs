using System.Text.Json.Serialization;
using Bookstore.Ordering;

namespace Bookstore.IO.WhatsNew;

public sealed record BookDto(string Isbn, string Title, string? Subtitle);

public sealed class CustomerProfile
{
    public string Name { get; set; } = "";
    public List<string> Tags { get; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CardPayment), "card")]
[JsonDerivedType(typeof(BankTransfer), "bank-transfer")]
public abstract record PaymentMethod;
public sealed record CardPayment(string Last4) : PaymentMethod;
public sealed record BankTransfer(string Reference) : PaymentMethod;

public sealed class Category
{
    public string Name { get; set; } = "";
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];
}

[JsonSerializable(typeof(Order))]
public partial class OrderContext : JsonSerializerContext;

// .NET 10: source generation ile ReferenceHandler.
[JsonSourceGenerationOptions(ReferenceHandler = JsonKnownReferenceHandler.Preserve)]
[JsonSerializable(typeof(Category))]
public partial class CategoryContext : JsonSerializerContext;
