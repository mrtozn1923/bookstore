using System.Globalization;
using BenchmarkDotNet.Attributes;
using Bookstore.IO.Csv.Export;
using nietras.SeparatedValues;
using Sylvan.Data.Csv;

namespace Bookstore.IO.Benchmarks;

[MemoryDiagnoser]
public class CsvBenchmarks
{
    private byte[] _data = [];

    [Params(100_000)]
    public int Orders { get; set; }

    [GlobalSetup]
    public void Setup() => _data = OrderSummaryRow.CreateCsv(Orders);

    // Yanlış ama sık görülen yöntem: tırnaklı alanları bilmez.
    [Benchmark(Baseline = true)]
    public decimal StringSplit()
    {
        using var reader = new StreamReader(new MemoryStream(_data));
        reader.ReadLine();
        decimal total = 0;
        while (reader.ReadLine() is { } line)
        {
            total += decimal.Parse(line.Split(',')[4], CultureInfo.InvariantCulture);
        }
        return total;
    }

    [Benchmark]
    public decimal CsvHelperRecords()
    {
        using var csv = CsvHelper.CsvReader.Create(new MemoryStream(_data), CsvHelper.CsvReaderOptions.Default);
        decimal total = 0;
        foreach (OrderSummaryRow row in csv.GetRecords<OrderSummaryRow>()) total += row.Total;
        return total;
    }

    [Benchmark]
    public decimal CsvHelperFields()
    {
        using var csv = CsvHelper.CsvReader.Create(new MemoryStream(_data), CsvHelper.CsvReaderOptions.Default);
        decimal total = 0;
        while (csv.Read()) total += csv.Parse<decimal>(4);
        return total;
    }

    [Benchmark]
    public decimal Sep()
    {
        using var reader = nietras.SeparatedValues.Sep.Reader().From(new MemoryStream(_data));
        int index = reader.Header.IndexOf("Total");
        decimal total = 0;
        foreach (var row in reader) total += row[index].Parse<decimal>();
        return total;
    }

    [Benchmark]
    public decimal Sylvan()
    {
        using var reader = CsvDataReader.Create(new StreamReader(new MemoryStream(_data)));
        int ordinal = reader.GetOrdinal("Total");
        decimal total = 0;
        while (reader.Read()) total += reader.GetDecimal(ordinal);
        return total;
    }
}
