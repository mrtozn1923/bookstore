using System.Text.Json.Serialization;

namespace Bookstore.IO.Json.Payments;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CardPayment), "card")]
[JsonDerivedType(typeof(BankTransferPayment), "bank-transfer")]
public abstract record PaymentMethod;

public sealed record CardPayment(string Brand, string Last4) : PaymentMethod;

public sealed record BankTransferPayment(string Iban, string Reference) : PaymentMethod;

public enum PaymentStatus
{
    Authorized,
    Captured,
    [JsonStringEnumMemberName("refund-pending")]
    RefundPending
}

public sealed class PaymentRequest
{
    public required string OrderNumber { get; init; }
    public required Money Amount { get; init; }
    public required PaymentMethod Method { get; init; }
    public PaymentStatus Status { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? Note { get; init; }
}

public sealed record Customer(string Name, string Email)
{
    [SensitiveData]
    public string? Phone { get; init; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveDataAttribute : Attribute;
