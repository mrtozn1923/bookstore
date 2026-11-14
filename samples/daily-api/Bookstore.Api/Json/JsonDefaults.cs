using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore.Api.Json;

public static class JsonDefaults
{
    // Minimal API ve controller'lar aynı ayarları buradan alır
    public static void Apply(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.TypeInfoResolverChain.Insert(0, BookstoreJsonContext.Default);
    }
}
