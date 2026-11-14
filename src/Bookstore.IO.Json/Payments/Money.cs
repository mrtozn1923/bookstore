using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore.IO.Json.Payments;

[JsonConverter(typeof(MoneyJsonConverter))]
public readonly record struct Money(decimal Amount, string Currency)
{
    public override string ToString() => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";
}

// Money travels as a single string: "249.90 TRY".
public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Money must be a string like \"249.90 TRY\", got {reader.TokenType}.");
        }

        string text = reader.GetString()!;
        int space = text.IndexOf(' ');
        if (space <= 0
            || !decimal.TryParse(text.AsSpan(0, space), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount)
            || text.Length - space - 1 != 3)
        {
            throw new JsonException($"Invalid money value \"{text}\".");
        }
        return new Money(amount, text[(space + 1)..]);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
