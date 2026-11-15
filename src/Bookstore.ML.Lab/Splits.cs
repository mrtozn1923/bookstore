using System.Diagnostics;
using System.Globalization;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Bookstore.ML.Lab;

/// <summary>Post 03: train/test splits, baselines, leakage and cross-validation.</summary>
public static class Splits
{
    private static readonly MLContext Ml = new(seed: 1);

    /// <summary>Delivered orders only: cancelled orders have no label.</summary>
    public static List<DeliveryInput> LoadDelivered()
    {
        IDataView raw = Ml.Data.LoadFromTextFile<DeliveryInput>("data/orders_clean.csv", hasHeader: true, separatorChar: ',');
        return [.. Ml.Data.CreateEnumerable<DeliveryInput>(raw, reuseRowObject: false).Where(o => !o.IsCancelled)];
    }

    /// <summary>A small tree model; post 04 explains every step of this pipeline.</summary>
    public static IEstimator<ITransformer> Pipeline(string[] numericFeatures, int leaves = 20, int trees = 100, int minLeaf = 10) =>
        Ml.Transforms.Conversion.ConvertType([new("Campaign", "IsCampaign"), new("Backorder", "HasBackorderItem")], DataKind.Single)
            .Append(Ml.Transforms.Categorical.OneHotEncoding([new("CarrierKey", "Carrier"), new("MethodKey", "ShippingMethod"),
                new("WarehouseKey", "Warehouse")]))
            .Append(Ml.Transforms.Concatenate("Features", ["CarrierKey", "MethodKey", "WarehouseKey", "Campaign", "Backorder",
                .. numericFeatures]))
            .Append(Ml.Regression.Trainers.FastTree(labelColumnName: "DeliveryDays", numberOfLeaves: leaves,
                numberOfTrees: trees, minimumExampleCountPerLeaf: minLeaf));

    private static readonly string[] Basic = ["DistanceKm", "ItemCount"];

    private static double Mae(ITransformer model, IDataView test) =>
        Ml.Regression.Evaluate(model.Transform(test), labelColumnName: "DeliveryDays").MeanAbsoluteError;

    public static void Run()
    {
        List<DeliveryInput> rows = LoadDelivered();
        IDataView all = Ml.Data.LoadFromEnumerable(rows);

        // 1) Random 80/20 split and two baselines
        DataOperationsCatalog.TrainTestData random = Ml.Data.TrainTestSplit(all, testFraction: 0.2, seed: 7);
        float trainMean = Ml.Data.CreateEnumerable<DeliveryInput>(random.TrainSet, false).Average(o => o.DeliveryDays);
        List<DeliveryInput> randomTest = [.. Ml.Data.CreateEnumerable<DeliveryInput>(random.TestSet, false)];
        Console.WriteLine($"rows: {rows.Count:N0}, train {random.TrainSet.GetRowCount() ?? Count(random.TrainSet):N0}, " +
                          $"test {randomTest.Count:N0}");
        Console.WriteLine($"baseline 'always {trainMean:F2} days'  MAE {randomTest.Average(o => Math.Abs(o.DeliveryDays - trainMean)):F3}");
        double ruleMae = randomTest.Average(o => Math.Abs(Rule(o) - o.DeliveryDays));
        Console.WriteLine($"baseline rule from post 01      MAE {ruleMae:F3}");
        ITransformer model = Pipeline(Basic).Fit(random.TrainSet);
        Console.WriteLine($"tree model, random split       MAE {Mae(model, random.TestSet):F3}");

        // 2) Target leakage: TrackingEventCount is only known after delivery
        ITransformer leaky = Pipeline([.. Basic, "TrackingEventCount"]).Fit(random.TrainSet);
        Console.WriteLine($"+ TrackingEventCount (leak)    MAE {Mae(leaky, random.TestSet):F3}");

        // 3) Time: train on the past, test on the last six months
        DateTime cut = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        IDataView past = Ml.Data.LoadFromEnumerable(rows.Where(o => o.OrderedAtUtc < cut));
        List<DeliveryInput> future = [.. rows.Where(o => o.OrderedAtUtc >= cut)];
        ITransformer timeModel = Pipeline(Basic).Fit(past);
        Console.WriteLine($"tree model, time split         MAE {Mae(timeModel, Ml.Data.LoadFromEnumerable(future)):F3} " +
                          $"(train {Count(past):N0} before {cut:yyyy-MM-dd}, test {future.Count:N0})");
        PredictionEngine<DeliveryInput, Prediction> engine = Ml.Model.CreatePredictionEngine<DeliveryInput, Prediction>(timeModel);
        foreach (var g in future.GroupBy(o => (o.OrderedAtUtc.Month, o.Carrier == "CarrierB")).OrderBy(g => g.Key))
        {
            double bias = g.Average(o => engine.Predict(o).Score - o.DeliveryDays);
            if (g.Key.Item2 || g.Key.Month is 5 or 10)
                Console.WriteLine($"  2026-{g.Key.Month:00} {(g.Key.Item2 ? "CarrierB" : "others  ")}  mean error {bias,6:F2} days ({g.Count()} orders)");
        }

        // 3b) The order date as a feature: a random split lets the model peek at the future
        List<DeliveryExtra> dated = [.. rows.Select(o => With(o, 0))];
        var datedRandom = Ml.Data.TrainTestSplit(Ml.Data.LoadFromEnumerable(dated), testFraction: 0.2, seed: 7);
        ITransformer dr = Pipeline([.. Basic, "DaySerial"]).Fit(datedRandom.TrainSet);
        List<DeliveryExtra> drTest = [.. Ml.Data.CreateEnumerable<DeliveryExtra>(datedRandom.TestSet, false)];
        ITransformer dt = Pipeline([.. Basic, "DaySerial"]).Fit(Ml.Data.LoadFromEnumerable(dated.Where(o => o.OrderedAtUtc < cut)));
        List<DeliveryExtra> dtTest = [.. dated.Where(o => o.OrderedAtUtc >= cut)];
        Console.WriteLine($"+ DaySerial, random split      MAE {Mae(dr, datedRandom.TestSet):F3}, CarrierB since June bias " +
                          $"{BiasB(dr, drTest):F2}");
        Console.WriteLine($"+ DaySerial, time split        MAE {Mae(dt, Ml.Data.LoadFromEnumerable(dtTest)):F3}, CarrierB since June bias " +
                          $"{BiasB(dt, dtTest):F2}");

        PredictionEngine<DeliveryInput, Prediction> randomEngine = Ml.Model.CreatePredictionEngine<DeliveryInput, Prediction>(model);
        List<DeliveryInput> lateB = [.. randomTest.Where(o => o.Carrier == "CarrierB" && o.OrderedAtUtc >= new DateTime(2026, 6, 1))];
        Console.WriteLine($"  random-split model on its own test rows, CarrierB since June: mean error " +
                          $"{lateB.Average(o => randomEngine.Predict(o).Score - o.DeliveryDays):F2} days ({lateB.Count} orders)");

        // 4) A feature computed on all rows: the customer's average delivery time
        List<DeliveryInput> train = [.. Ml.Data.CreateEnumerable<DeliveryInput>(random.TrainSet, false)];
        Dictionary<string, float> avgAll = rows.GroupBy(o => o.CustomerId).ToDictionary(g => g.Key, g => g.Average(o => o.DeliveryDays));
        Dictionary<string, float> avgTrain = train.GroupBy(o => o.CustomerId).ToDictionary(g => g.Key, g => g.Average(o => o.DeliveryDays));
        foreach ((Dictionary<string, float> avg, string name) in new[] { (avgAll, "all rows (leak)"), (avgTrain, "train rows only") })
        {
            IDataView tr = Ml.Data.LoadFromEnumerable(train.Select(o => With(o, avg.GetValueOrDefault(o.CustomerId, trainMean))));
            IDataView te = Ml.Data.LoadFromEnumerable(randomTest.Select(o => With(o, avg.GetValueOrDefault(o.CustomerId, trainMean))));
            ITransformer m = Pipeline([.. Basic, "CustomerAvgDays"]).Fit(tr);
            Console.WriteLine($"+ CustomerAvgDays from {name,-16} MAE {Mae(m, te):F3}");
        }

        // 5) The same customer on both sides of the split
        DataOperationsCatalog.TrainTestData grouped = Ml.Data.TrainTestSplit(all, testFraction: 0.2, samplingKeyColumnName: "CustomerId", seed: 7);
        Console.WriteLine($"customers in both train and test: random {Overlap(random):N0}, " +
                          $"samplingKeyColumnName {Overlap(grouped):N0}");
    }

    public static void CrossValidation()
    {
        List<DeliveryInput> rows = LoadDelivered();
        IDataView all = Ml.Data.LoadFromEnumerable(rows);
        Stopwatch sw = Stopwatch.StartNew();
        var folds = Ml.Regression.CrossValidate(all, Pipeline(Basic), numberOfFolds: 5, labelColumnName: "DeliveryDays",
            samplingKeyColumnName: "CustomerId", seed: 7);
        double[] maes = [.. folds.Select(f => f.Metrics.MeanAbsoluteError)];
        Console.WriteLine("5-fold MAE: " + string.Join("  ", maes.Select(m => m.ToString("F3", CultureInfo.InvariantCulture))));
        Console.WriteLine($"mean {maes.Average():F3}, std {Std(maes):F3}");

        // A small dataset: one split is a lottery
        IDataView small = Ml.Data.TakeRows(Ml.Data.ShuffleRows(all, seed: 3), 400);
        double[] single = [.. Enumerable.Range(1, 10).Select(seed =>
        {
            var s = Ml.Data.TrainTestSplit(small, testFraction: 0.2, seed: seed);
            return Mae(Pipeline(Basic, minLeaf: 5).Fit(s.TrainSet), s.TestSet);
        })];
        Console.WriteLine($"400 rows, 10 different 80/20 splits: MAE min {single.Min():F3}, max {single.Max():F3}");
        var smallCv = Ml.Regression.CrossValidate(small, Pipeline(Basic, minLeaf: 5), numberOfFolds: 5, labelColumnName: "DeliveryDays", seed: 7);
        double[] cvMaes = [.. smallCv.Select(f => f.Metrics.MeanAbsoluteError)];
        Console.WriteLine($"400 rows, 5-fold CV: mean {cvMaes.Average():F3}, std {Std(cvMaes):F3}");
    }

    public static void Complexity()
    {
        List<DeliveryInput> rows = LoadDelivered();
        IDataView all = Ml.Data.ShuffleRows(Ml.Data.LoadFromEnumerable(rows), seed: 5);
        IDataView train = Ml.Data.TakeRows(all, 1500);
        IDataView validation = Ml.Data.TakeRows(Ml.Data.SkipRows(all, 1500), 5000);
        string[] features = [.. Basic, "TotalWeightKg", "OrderTotal"];
        List<string> lines = ["leaves,train_mae,validation_mae"];
        foreach (int leaves in new[] { 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128 })
        {
            ITransformer m = Pipeline(features, leaves: leaves, trees: 10, minLeaf: 1).Fit(train);
            double tr = Mae(m, train), va = Mae(m, validation);
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"{leaves},{tr:F4},{va:F4}"));
            Console.WriteLine($"leaves {leaves,3}: train MAE {tr:F3}  validation MAE {va:F3}");
        }
        Directory.CreateDirectory("out");
        File.WriteAllLines("out/03-complexity.csv", lines);
    }

    private static DeliveryExtra With(DeliveryInput o, float avg) => new()
    {
        OrderId = o.OrderId, CustomerId = o.CustomerId, OrderedAtUtc = o.OrderedAtUtc, City = o.City, District = o.District,
        Warehouse = o.Warehouse, DistanceKm = o.DistanceKm, Carrier = o.Carrier, ShippingMethod = o.ShippingMethod,
        ItemCount = o.ItemCount, TotalWeightKg = o.TotalWeightKg, OrderTotal = o.OrderTotal, IsCampaign = o.IsCampaign,
        PaymentMethod = o.PaymentMethod, HasBackorderItem = o.HasBackorderItem, CustomerNote = o.CustomerNote,
        DeliveryDays = o.DeliveryDays, TrackingEventCount = o.TrackingEventCount, CustomerAvgDays = avg,
        DaySerial = (float)(o.OrderedAtUtc - new DateTime(2024, 11, 1)).TotalDays,
    };

    private static double BiasB(ITransformer model, List<DeliveryExtra> test)
    {
        PredictionEngine<DeliveryExtra, Prediction> e = Ml.Model.CreatePredictionEngine<DeliveryExtra, Prediction>(model);
        return test.Where(o => o.Carrier == "CarrierB" && o.OrderedAtUtc >= new DateTime(2026, 6, 1))
            .Average(o => e.Predict(o).Score - o.DeliveryDays);
    }

    private static double Rule(DeliveryInput o)
    {
        double days = o.DistanceKm switch { < 100 => 1, < 500 => 2, < 1000 => 3, _ => 4 };
        if (o.ShippingMethod == "Express") days -= 1;
        if (o.HasBackorderItem) days += 5;
        return Math.Max(1, days);
    }

    private static int Overlap(DataOperationsCatalog.TrainTestData split)
    {
        HashSet<string> train = [.. Ml.Data.CreateEnumerable<DeliveryInput>(split.TrainSet, false).Select(o => o.CustomerId)];
        return Ml.Data.CreateEnumerable<DeliveryInput>(split.TestSet, false).Select(o => o.CustomerId).Distinct().Count(train.Contains);
    }

    private static long Count(IDataView view) => view.GetRowCount() ?? Ml.Data.CreateEnumerable<DeliveryInput>(view, false).LongCount();

    private static double Std(double[] x)
    {
        double mean = x.Average();
        return Math.Sqrt(x.Sum(v => (v - mean) * (v - mean)) / (x.Length - 1));
    }
}

public sealed class Prediction
{
    public float Score { get; set; }
}
