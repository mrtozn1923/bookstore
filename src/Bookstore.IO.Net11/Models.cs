using System.Text.Json.Serialization;

namespace Bookstore.IO.Net11;

// C# 15 union: bir sipariş referansı ya sayısal ID ya da "BS-..." numarası.
public union OrderReference(int, string);

public sealed record PaymentApproved(string AuthorizationCode, decimal Amount);
public sealed record PaymentDeclined(string Reason, int RetryAfterSeconds);

public union PaymentResultByKind(PaymentApproved, PaymentDeclined);

[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union PaymentResult(PaymentApproved, PaymentDeclined);

// C# 15 closed hierarchy: türetilmiş tipler derleme anında bilinir.
[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record ShippingOption;
public sealed record StandardShipping(int Days) : ShippingOption;
public sealed record ExpressShipping(DateOnly DeliverBy) : ShippingOption;

[JsonNamingPolicy(JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public sealed class StockAlert
{
    public string Isbn { get; set; } = "";
    public int QuantityOnHand { get; set; }
    public string? WarehouseNote { get; set; }
}
