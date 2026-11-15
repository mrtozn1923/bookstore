using Microsoft.ML;
using Microsoft.ML.Data;

namespace Bookstore.ML.Lab;

/// <summary>Post 04: the errors you meet in the first week, reproduced on purpose.</summary>
public static class Pitfalls
{
    /// <summary>The same input, but DistanceKm declared as double.</summary>
    public sealed class DoubleInput
    {
        public string Carrier { get; set; } = "";
        public string ShippingMethod { get; set; } = "";
        public string Warehouse { get; set; } = "";
        public double DistanceKm { get; set; }
        public float ItemCount { get; set; }
        public bool IsCampaign { get; set; }
        public bool HasBackorderItem { get; set; }
    }

    public static void Run()
    {
        MLContext ml = new(seed: 1);
        ITransformer model = ml.Model.Load(FirstModel.ModelPath, out _);

        // 1) double instead of float
        try
        {
            var engine = ml.Model.CreatePredictionEngine<DoubleInput, DeliveryPrediction>(model);
            Console.WriteLine(engine.Predict(new DoubleInput { DistanceKm = 450 }).PredictedDays);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[double] {ex.GetType().Name}: {ex.Message}");
        }

        // 2) A typo in a column name is only noticed when Fit runs
        IDataView data = ml.Data.LoadFromTextFile<DeliveryInput>("data/orders_clean.csv", hasHeader: true, separatorChar: ',');
        var typo = ml.Transforms.Concatenate("Features", "DistanceKM", "ItemCount")
            .Append(ml.Regression.Trainers.Sdca(labelColumnName: "DeliveryDays"));
        try
        {
            typo.Fit(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[typo]   {ex.GetType().Name}: {ex.Message}");
        }

        // 3) Empty cells: the default loader reads them as 0, not as missing
        IDataView labels = ml.Data.FilterRowsByMissingValues(data, "DeliveryDays");
        float[] days = [.. labels.GetColumn<float>("DeliveryDays")];
        float[] weights = [.. labels.GetColumn<float>("TotalWeightKg")];
        Console.WriteLine($"[empty]  default loader: {days.Length:N0} rows survive the filter, " +
                          $"{days.Count(d => d == 0):N0} have DeliveryDays = 0, {weights.Count(w => w == 0):N0} have weight 0");

        // 4) One PredictionEngine shared by many threads
        PredictionEngine<DeliveryInput, DeliveryPrediction> shared =
            ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(model);
        DeliveryInput[] orders = [.. Enumerable.Range(0, 2000).Select(i => new DeliveryInput
        {
            Carrier = "CarrierA", ShippingMethod = "Standard", Warehouse = "IST-1", DistanceKm = 20 + i % 1500, ItemCount = 1 + i % 5,
        })];
        float[] expected = [.. orders.Select(o => shared.Predict(o).PredictedDays)];
        int wrong = 0, failed = 0;
        string? firstError = null;
        Parallel.For(0, orders.Length, i =>
        {
            try
            {
                if (shared.Predict(orders[i]).PredictedDays != expected[i]) Interlocked.Increment(ref wrong);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                Interlocked.CompareExchange(ref firstError, ex.GetType().Name, null);
            }
        });
        Console.WriteLine($"[threads] 2000 parallel predictions on one engine: {wrong} wrong, {failed} exceptions (first: {firstError})");
    }
}
