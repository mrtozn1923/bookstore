using System.Text.Json;
using System.Text.Json.Serialization;
using Bookstore.Ordering;

namespace Bookstore.IO.Json;

// Source-generated metadata and fast-path serializers for the export types.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Order>))]
[JsonSerializable(typeof(IAsyncEnumerable<Order>))]
[JsonSerializable(typeof(ExportSummary))]
public partial class BookstoreJsonContext : JsonSerializerContext;

public sealed record ExportSummary(long Orders, decimal Revenue, int Customers);
