using System.Diagnostics;
using System.Globalization;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

// Usage (from the repo root, after python/cancel_model.py): dotnet run --project src/Bookstore.ML.Onnx
CultureInfo inv = CultureInfo.InvariantCulture;
string[] categorical = ["PaymentMethod", "ShippingMethod", "Warehouse"];
string[] numeric = ["ItemCount", "OrderTotal", "DiscountRate", "PreviousOrders", "IsCampaign", "HasBackorderItem", "IsFirstOrder"];

string[] header = File.ReadLines("models/cancel_test.csv").First().Split(',');
string[][] rows = [.. File.ReadLines("models/cancel_test.csv").Skip(1).Select(line => line.Split(','))];
int Col(string name) => Array.IndexOf(header, name);

using InferenceSession session = new("models/cancel.onnx");
foreach ((string name, NodeMetadata meta) in session.InputMetadata.Where(i => i.Key is "PaymentMethod" or "OrderTotal"))
{
    Console.WriteLine($"input  {name,-14} {meta.ElementType.Name}[{string.Join(", ", meta.Dimensions)}]");
}
foreach ((string name, NodeMetadata meta) in session.OutputMetadata)
{
    Console.WriteLine($"output {name,-14} {meta.ElementType.Name}[{string.Join(", ", meta.Dimensions)}]");
}

// One tensor per column, shape [rows, 1], the same names and types as initial_types in Python
List<NamedOnnxValue> Feed(string[][] batch) =>
[
    .. categorical.Select(c => NamedOnnxValue.CreateFromTensor(c,
        new DenseTensor<string>(batch.Select(r => r[Col(c)]).ToArray(), [batch.Length, 1]))),
    .. numeric.Select(c => NamedOnnxValue.CreateFromTensor(c,
        new DenseTensor<float>(batch.Select(r => float.Parse(r[Col(c)], inv)).ToArray(), [batch.Length, 1]))),
];

using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(Feed(rows)))
{
    Tensor<float> probabilities = results.First(r => r.Name == "probabilities").AsTensor<float>();
    double[] onnx = [.. Enumerable.Range(0, rows.Length).Select(i => (double)probabilities[i, 1])];
    double[] sklearn = [.. rows.Select(r => double.Parse(r[Col("SklearnProbability")], inv))];
    double maxDiff = onnx.Zip(sklearn).Max(p => Math.Abs(p.First - p.Second));
    int decisions = onnx.Zip(sklearn).Count(p => p.First >= 0.06 != p.Second >= 0.06);
    Console.WriteLine($"{rows.Length:N0} test rows: max |.NET ONNX - scikit-learn| = {maxDiff:E2}, " +
                      $"different decisions at t = 0.06: {decisions}");
    for (int i = 0; i < 3; i++)
    {
        Console.WriteLine($"  {rows[i][Col("PaymentMethod")],-15} back-order {rows[i][Col("HasBackorderItem")]}  " +
                          $"scikit-learn {sklearn[i]:F6}  .NET {onnx[i]:F6}");
    }
}

// One order at a time, as an API would call it
Stopwatch watch = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> one = session.Run(Feed([rows[i]]));
}
Console.WriteLine($"single-row Run: {watch.Elapsed.TotalMicroseconds / 1000:F0} µs per call (1,000 calls)");

// The graph expects float32; double is not converted for you
try
{
    List<NamedOnnxValue> wrong = Feed([rows[0]]);
    wrong[categorical.Length + 1] = NamedOnnxValue.CreateFromTensor("OrderTotal", new DenseTensor<double>(new[] { 389.8 }, [1, 1]));
    session.Run(wrong);
}
catch (OnnxRuntimeException e)
{
    Console.WriteLine($"double input: {e.GetType().Name}: {e.Message.Split('\n')[0]}");
}
