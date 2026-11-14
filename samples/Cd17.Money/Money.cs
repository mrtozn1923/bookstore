using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore.Billing.Money;

public readonly record struct Currency(string Code, int MinorUnits)
{
    public static readonly Currency TRY = new("TRY", 2);
    public static readonly Currency EUR = new("EUR", 2);
    public static readonly Currency JPY = new("JPY", 0);

    public static Currency FromCode(string code) => code switch
    {
        "TRY" => TRY,
        "EUR" => EUR,
        "JPY" => JPY,
        _ => throw new ArgumentException($"Unknown currency '{code}'.", nameof(code))
    };
}

public readonly record struct Money(decimal Amount, Currency Currency)
{
    public static Money Try(decimal amount) => new(amount, Currency.TRY);

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount, Same(a, b));
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount, Same(a, b));
    public static Money operator *(Money a, decimal factor) => new(a.Amount * factor, a.Currency);

    private static Currency Same(Money a, Money b) => a.Currency == b.Currency
        ? a.Currency
        : throw new InvalidOperationException($"Cannot combine {a.Currency.Code} and {b.Currency.Code}.");

    public Money Round(MidpointRounding mode = MidpointRounding.AwayFromZero) =>
        new(Math.Round(Amount, Currency.MinorUnits, mode), Currency);

    // Fowler's allocate from the Money pattern: leftover cents go one by one from the start
    public Money[] Allocate(params int[] ratios)
    {
        decimal unit = 1m / (decimal)Math.Pow(10, Currency.MinorUnits);
        long totalUnits = (long)(Round().Amount / unit);
        int ratioSum = ratios.Sum();

        long[] shares = [.. ratios.Select(r => totalUnits * r / ratioSum)];
        long remainder = totalUnits - shares.Sum();
        for (int i = 0; remainder > 0; i = (i + 1) % shares.Length, remainder--)
            shares[i]++;

        Currency currency = Currency;
        return [.. shares.Select(s => new Money(s * unit, currency))];
    }

    public override string ToString() =>
        $"{Amount.ToString("F" + Currency.MinorUnits, CultureInfo.InvariantCulture)} {Currency.Code}";
}

public sealed record InvoiceLine(Money Net, decimal VatRate);

public static class Vat
{
    // Per line: each line's VAT is rounded separately
    public static Money[] PerLine(IReadOnlyList<InvoiceLine> lines) =>
        [.. lines.Select(l => (l.Net * l.VatRate).Round())];

    // From the total: rounded once per rate group, the difference is spread over the lines
    public static Money[] FromTotal(IReadOnlyList<InvoiceLine> lines)
    {
        Money[] result = new Money[lines.Count];
        foreach (IGrouping<decimal, int> group in Enumerable.Range(0, lines.Count).GroupBy(i => lines[i].VatRate))
        {
            int[] idx = [.. group];
            Money groupNet = idx.Select(i => lines[i].Net).Aggregate((a, b) => a + b);
            Money groupVat = (groupNet * group.Key).Round();
            int[] weights = [.. idx.Select(i => (int)(lines[i].Net.Amount * 100))];
            Money[] shares = groupVat.Allocate(weights);
            for (int k = 0; k < idx.Length; k++) result[idx[k]] = shares[k];
        }
        return result;
    }
}

public enum VatCategory { General, Reduced, Basic }

public sealed record VatRate(VatCategory Category, DateOnly ValidFrom, DateOnly? ValidTo, decimal Rate);

public sealed class VatRateTable(IReadOnlyList<VatRate> rates)
{
    public decimal For(VatCategory category, DateOnly date) =>
        rates.Single(r => r.Category == category && r.ValidFrom <= date && (r.ValidTo is null || date < r.ValidTo)).Rate;
}

public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        decimal amount = decimal.Parse(doc.RootElement.GetProperty("amount").GetString()!, CultureInfo.InvariantCulture);
        return new Money(amount, Currency.FromCode(doc.RootElement.GetProperty("currency").GetString()!));
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("amount", value.Amount.ToString(CultureInfo.InvariantCulture));
        writer.WriteString("currency", value.Currency.Code);
        writer.WriteEndObject();
    }
}
