using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Bookstore.Web.StructuredData;

public static class JsonLd
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Turkish letters stay readable; < > & ' " are still escaped as < etc.
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static string Serialize(JsonLdDocument document) => JsonSerializer.Serialize(document, Options);
}
