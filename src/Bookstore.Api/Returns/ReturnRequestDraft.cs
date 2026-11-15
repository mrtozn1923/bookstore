using System.ComponentModel;

namespace Bookstore.Api.Returns;

public enum Urgency
{
    Low,
    Normal,
    High,
}

public sealed record ReturnItem(
    [property: Description("Book title as the customer wrote it.")] string Title,
    [property: Description("Number of copies to return.")] int Quantity);

public sealed record ReturnRequestDraft(
    [property: Description("Order number if the customer gave one, otherwise null.")] string? OrderId,
    ReturnReason Reason,
    IReadOnlyList<ReturnItem> Items,
    Urgency Urgency,
    [property: Description("One sentence in English for the support team.")] string Summary);
