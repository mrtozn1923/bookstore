namespace Bookstore.Privacy;

public sealed record ProcessingActivity(
    string Category,
    string Purpose,
    string LegalBasis,
    string Retention,
    string[] Roles,
    string[] Copies);

// Values come from the legal team; retention periods below are examples, not advice.
public static class ProcessingActivities
{
    public static readonly ProcessingActivity[] All =
    [
        new("customer-profile", "Account and order communication", "Contract",
            "While the account is open, then erased", ["Customer", "StoreStaff"], ["db", "backup", "search-index", "email-service"]),
        new("order-billing", "Invoicing and order history", "Legal obligation",
            "10 years after the year of the order (TTK 82)", ["Customer", "StoreStaff", "Accounting"], ["db", "backup", "accounting-export"]),
        new("sign-in-session", "Account security, fraud detection", "Legitimate interest",
            "Example: 180 days", ["Admin"], ["db", "backup", "logs"]),
        new("payment", "Payment reconciliation and refunds", "Contract",
            "Same as order-billing", ["Accounting"], ["db", "backup", "payment-provider"]),
        new("institution-user", "Institutional orders and approvals", "Ask legal",
            "While the institution's contract is active", ["InstitutionApprover", "StoreStaff"], ["db", "backup"]),
        // Logs, traces and browser error reports: no table of their own, but the same questions apply.
        new("telemetry", "Operations, debugging, security monitoring", "Legitimate interest",
            "Example: logs 30 days, traces 7 days, error reports 30 days", ["Developer", "SRE"], ["log-store", "trace-store", "error-reports"]),
        new("privacy-request", "Handling data subject requests", "Legal obligation",
            "Example: 3 years after completion", ["PrivacyOfficer"], ["db", "backup"]),
    ];
}
