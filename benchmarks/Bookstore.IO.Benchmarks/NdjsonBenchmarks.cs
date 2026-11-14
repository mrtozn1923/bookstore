using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Bookstore.IO.Pipelines;
using Bookstore.Ordering;

namespace Bookstore.IO.Benchmarks;

[MemoryDiagnoser]
public class NdjsonBenchmarks
{
    private byte[] _data = [];
    private decimal _total;

    [Params(10_000)]
    public int Orders { get; set; }

    [GlobalSetup]
    public void Setup() => _data = PipeDemos.CreateNdjson(Orders);

    private ValueTask Sum(Order order)
    {
        _total += order.Total;
        return ValueTask.CompletedTask;
    }

    [Benchmark(Baseline = true)]
    public Task<long> StreamReader() =>
        NdjsonOrders.ReadWithStreamReaderAsync(new MemoryStream(_data), Sum);

    [Benchmark]
    public Task<long> PipeReader() =>
        NdjsonOrders.ReadWithPipeReaderAsync(System.IO.Pipelines.PipeReader.Create(new MemoryStream(_data)), Sum);

    // Line splitting only, no JSON: shows the cost of turning every line into a string.
    [Benchmark]
    public async Task<long> StreamReaderLinesOnly()
    {
        using var reader = new StreamReader(new MemoryStream(_data));
        long lines = 0;
        while (await reader.ReadLineAsync() is not null) lines++;
        return lines;
    }

    [Benchmark]
    public async Task<long> PipeReaderLinesOnly()
    {
        var reader = System.IO.Pipelines.PipeReader.Create(new MemoryStream(_data));
        long lines = 0;
        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;
            SequencePosition? position;
            while ((position = buffer.PositionOf((byte)'\n')) is not null)
            {
                lines++;
                buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            }
            reader.AdvanceTo(buffer.Start, buffer.End);
            if (result.IsCompleted) break;
        }
        await reader.CompleteAsync();
        return lines;
    }
}
