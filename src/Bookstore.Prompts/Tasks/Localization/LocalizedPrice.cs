using System.Globalization;

namespace Bookstore.Prompts.Tasks.Localization;

// Prices and dates are formatted by code per language and passed in as placeholders
public static class LocalizedPrice
{
    static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static string Format(decimal amount, string language) => language switch
    {
        "tr" => string.Create(Tr, $"{amount:N2} TL"),
        _ => string.Create(En, $"TRY {amount:N2}"),
    };

    public static string FormatDate(DateOnly date, string language) => language switch
    {
        "tr" => date.ToString("d MMMM yyyy", Tr),
        _ => date.ToString("MMMM d, yyyy", En),
    };
}
