using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using Bookstore.Ordering;

namespace Bookstore.IO.Benchmarks;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<Order>))]
public partial class MetadataOnlyContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(List<Order>))]
public partial class FastPathContext : JsonSerializerContext;

[MemoryDiagnoser]
public class JsonSerializeBenchmarks
{
    private List<Order> _orders = [];
    private readonly Utf8JsonWriter _writer = new(Stream.Null);

    [GlobalSetup]
    public void Setup() => _orders = SampleOrders.Generate(1_000).ToList();

    [Benchmark(Baseline = true)]
    public void Reflection()
    {
        _writer.Reset();
        JsonSerializer.Serialize(_writer, _orders, JsonSerializerOptions.Web);
        _writer.Flush();
    }

    [Benchmark]
    public void SourceGenMetadata()
    {
        _writer.Reset();
        JsonSerializer.Serialize(_writer, _orders, MetadataOnlyContext.Default.ListOrder);
        _writer.Flush();
    }

    [Benchmark]
    public void SourceGenFastPath()
    {
        _writer.Reset();
        JsonSerializer.Serialize(_writer, _orders, FastPathContext.Default.ListOrder);
        _writer.Flush();
    }
}
