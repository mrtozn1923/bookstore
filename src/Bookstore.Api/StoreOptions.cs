namespace Bookstore.Api;

public sealed class StoreOptions
{
    public const string SectionName = "Store";

    public string Culture { get; set; } = "tr-TR";
    public string TimeZone { get; set; } = "Europe/Istanbul";
}
