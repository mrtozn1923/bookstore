using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore.IO.Json.Payments;

// For a partner that sends dates as "14.11.2026" instead of ISO 8601.
public sealed class DayMonthYearDateOnlyConverter : JsonConverter<DateOnly>
{
    private const string Format = "dd.MM.yyyy";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateOnly.ParseExact(reader.GetString()!, Format, CultureInfo.InvariantCulture);

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
