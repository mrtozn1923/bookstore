namespace Bookstore.Privacy.Transfers;

// Mechanisms of Law No. 6698 Article 9 and the 2024 transfer regulation.
public enum TransferMechanism { Domestic, AdequacyDecision, StandardContract, BindingCorporateRules, WrittenUndertaking, Occasional }

public sealed record SubProcessor(
    string Key,
    string Role,
    string Location,
    bool Abroad,
    string[] DataCategories,
    TransferMechanism Mechanism,
    string? ContractType = null,
    DateOnly? SignedOn = null,
    DateOnly? NotifiedOn = null);

// Values are examples; the legal team owns the contract type and the dates.
public static class SubProcessors
{
    public static readonly SubProcessor[] All =
    [
        new("cloud-hosting", "Application hosting, database, backups", "EU region (example)", true,
            ["customer-profile", "order-billing", "sign-in-session", "payment", "consent"],
            TransferMechanism.StandardContract, "Standard contract 2 (controller to processor)", new(2026, 10, 1), new(2026, 10, 6)),
        new("telemetry", "Logs and traces", "EU region (example)", true, ["telemetry"],
            TransferMechanism.StandardContract, "Standard contract 2 (controller to processor)", new(2026, 10, 1), new(2026, 10, 6)),
        new("email", "Order and campaign e-mails", "Abroad (example)", true, ["customer-profile"],
            TransferMechanism.StandardContract, "Standard contract 2 (controller to processor)", new(2026, 10, 12), new(2026, 10, 14)),
        new("analytics", "Web analytics (after consent)", "Abroad (example)", true, ["analytics-events"],
            TransferMechanism.StandardContract, "Ask legal: contract type", new(2026, 10, 12), new(2026, 10, 14)),
        // Book recommendations: the prompt carries purchased titles only, no name, e-mail or address.
        new("llm", "Book recommendations", "Abroad (example)", true, ["purchase-history-titles"],
            TransferMechanism.StandardContract, "Standard contract 2 (controller to processor)", new(2026, 11, 9), new(2026, 11, 13)),
        new("payment-provider", "Card payments and refunds", "Turkey (example)", false, ["payment"], TransferMechanism.Domestic),
        new("iys", "Commercial message permissions", "Turkey", false, ["commercial-message"], TransferMechanism.Domestic),
    ];
}
