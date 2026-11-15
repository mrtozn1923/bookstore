namespace Bookstore.Prompts.Tasks.TicketRouting;

public sealed record LabelDefinition(string Name, string Definition);

public static class TicketLabels
{
    public const string Human = "Human";

    // Each ticket belongs to exactly one team; the boundaries are part of the definition
    public static readonly IReadOnlyList<LabelDefinition> All =
    [
        new("Returns", "The customer wants to return or exchange a book they received (damaged, wrong, missing pages, changed their mind), or asks about the status of a return or of a refund for a returned book."),
        new("Shipping", "Delivery of an individual order that has not arrived yet: tracking, delays, courier problems, changing the address before dispatch. A book that arrived damaged is Returns, not Shipping."),
        new("Payments", "Charging and invoicing of an individual order: double charges, declined cards, installments, invoice details. A refund for a returned book is Returns, not Payments."),
        new("Institutional", "Any message from a school, library or company about an institutional order: quotes for 20 or more copies, institutional discount, purchase approval, and also delivery, damage or invoices of an institutional order."),
        new(Human, "Anything else: complaints about staff, legal threats, several unrelated problems in one message, unclear or empty messages, and questions no team above owns."),
    ];
}
