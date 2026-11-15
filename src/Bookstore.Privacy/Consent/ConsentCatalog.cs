namespace Bookstore.Privacy.Consent;

public sealed record ConsentPurpose(string Key, bool RequiresConsent, string Description);

// The legal team owns the texts and the notice version; the code only refuses decisions made on an old text.
public static class ConsentCatalog
{
    public const string NoticeVersion = "2026-11";
    public const string CommercialEmail = "commercial-email";

    public static readonly ConsentPurpose[] CookiePurposes =
    [
        new("necessary", false, "Session, cart, security and the consent choice itself"),
        new("analytics", true, "Third-party analytics cookies"),
        new("marketing", true, "Advertising and remarketing cookies"),
    ];

    public static bool IsCookiePurposeWithConsent(string key) =>
        CookiePurposes.Any(p => p.Key == key && p.RequiresConsent);
}
