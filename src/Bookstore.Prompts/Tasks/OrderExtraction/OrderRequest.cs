using System.ComponentModel;

namespace Bookstore.Prompts.Tasks.OrderExtraction;

// v1 schema: every field is required and non-nullable
public sealed record OrderRequestV1(List<RequestedItemV1> Items, string DeliveryDate);

public sealed record RequestedItemV1(string Title, string Isbn, int Quantity);

// v2 schema: nullable fields, descriptions and a source quote per item
public sealed record OrderRequest(
    [property: Description("One entry per book the sender wants to order now.")]
    List<RequestedItem> Items,
    [property: Description("The delivery date phrase copied exactly from the email, e.g. \"by next Tuesday\". null if no date is mentioned.")]
    string? DeliveryDateText,
    [property: Description("Your reading of DeliveryDateText as yyyy-MM-dd, using the Sent date. null if no date is mentioned.")]
    string? DeliveryDate);

public sealed record RequestedItem(
    [property: Description("Book title as written in the email.")]
    string Title,
    [property: Description("ISBN-13 digits only, only if written in the email body. Otherwise null.")]
    string? Isbn,
    [property: Description("Number of copies requested now. null if the email gives no exact number.")]
    int? Quantity,
    [property: Description("The sentence from the email this item comes from, copied word for word.")]
    string SourceQuote);
