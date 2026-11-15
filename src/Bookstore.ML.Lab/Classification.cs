using System.Globalization;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Bookstore.ML.Lab;

/// <summary>What the cancellation model may see: only facts known at the moment the order is placed.</summary>
public sealed class CancelInput
{
    public string OrderId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public DateTime OrderedAtUtc { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string ShippingMethod { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public float ItemCount { get; set; }
    public float OrderTotal { get; set; }
    public float DiscountRate { get; set; }
    public bool IsCampaign { get; set; }
    public bool HasBackorderItem { get; set; }
    public float PreviousOrders { get; set; }
    public bool IsFirstOrder { get; set; }
    public bool IsCancelled { get; set; }
}

public sealed class CancelScore
{
    public bool IsCancelled { get; set; }
    public float Probability { get; set; }
}

public sealed class ReviewInput
{
    [LoadColumn(0)] public string ReviewId { get; set; } = "";
    [LoadColumn(3)] public float Rating { get; set; }
    [LoadColumn(4)] public string Text { get; set; } = "";
    [LoadColumn(5)] public string Topic { get; set; } = "";
}

public sealed class TopicPrediction
{
    public string Text { get; set; } = "";
    public string Topic { get; set; } = "";
    public string PredictedTopic { get; set; } = "";
    public float[] Score { get; set; } = [];
}

/// <summary>Post 07: binary classification (will the order be cancelled?) and multiclass (what is the review about?).</summary>
public static class Classification
{
    /// <summary>orders_clean.csv in time order; PreviousOrders counts only the customer's earlier orders.</summary>
    public static List<CancelInput> LoadOrders()
    {
        Dictionary<string, int> seen = [];
        List<CancelInput> rows = [];
        foreach (OrderRow o in OrderRow.Load("data/orders_clean.csv").OrderBy(o => o.OrderedAtUtc))
        {
            int previous = seen.GetValueOrDefault(o.CustomerId);
            seen[o.CustomerId] = previous + 1;
            rows.Add(new CancelInput
            {
                OrderId = o.OrderId, CustomerId = o.CustomerId, OrderedAtUtc = o.OrderedAtUtc,
                PaymentMethod = o.PaymentMethod, ShippingMethod = o.ShippingMethod, Warehouse = o.Warehouse,
                ItemCount = o.ItemCount, OrderTotal = (float)o.OrderTotal, DiscountRate = (float)o.DiscountRate,
                IsCampaign = o.IsCampaign, HasBackorderItem = o.HasBackorderItem,
                PreviousOrders = previous, IsFirstOrder = previous == 0, IsCancelled = o.IsCancelled,
            });
        }
        return rows;
    }

    public static readonly string[] OrderFeatures =
        ["Payment", "Method", "WarehouseKey", "Campaign", "Backorder", "FirstOrder", "ItemCount", "OrderTotal",
         "DiscountRate", "PreviousOrders"];

    /// <summary>One-hot categories, booleans as 0/1, then mean/variance normalization for the linear model.</summary>
    public static IEstimator<ITransformer> Features(MLContext ml, string[]? extra = null) =>
        ml.Transforms.Categorical.OneHotEncoding(
                [new("Payment", "PaymentMethod"), new("Method", "ShippingMethod"), new("WarehouseKey", "Warehouse")])
            .Append(ml.Transforms.Conversion.ConvertType(
                [new("Campaign", "IsCampaign"), new("Backorder", "HasBackorderItem"), new("FirstOrder", "IsFirstOrder")],
                DataKind.Single))
            .Append(ml.Transforms.Concatenate("Features", [.. OrderFeatures, .. extra ?? []]))
            .Append(ml.Transforms.NormalizeMeanVariance("Features"));

    public static IEstimator<ITransformer> Logistic(MLContext ml) =>
        ml.BinaryClassification.Trainers.LbfgsLogisticRegression(new LbfgsLogisticRegressionBinaryTrainer.Options
        {
            LabelColumnName = "IsCancelled", FeatureColumnName = "Features", NumberOfThreads = 1,
        });

    public static void Cancel()
    {
        MLContext ml = new(seed: 1);
        IDataView all = ml.Data.LoadFromEnumerable(LoadOrders());

        // 60/20/20 grouped by customer: train, validation (for the threshold), test (reported once)
        var first = ml.Data.TrainTestSplit(all, testFraction: 0.2, samplingKeyColumnName: "CustomerId", seed: 7);
        // A different seed: with seed 7 again, validation got only 1,169 orders
        var second = ml.Data.TrainTestSplit(first.TrainSet, testFraction: 0.25, samplingKeyColumnName: "CustomerId", seed: 11);
        IDataView train = ml.Data.Cache(second.TrainSet), validation = second.TestSet, test = first.TestSet;

        foreach ((string name, IDataView set) in new[] { ("train", train), ("validation", validation), ("test", test) })
        {
            bool[] labels = [.. set.GetColumn<bool>("IsCancelled")];
            Console.WriteLine($"{name,-11}{labels.Length,7:N0} orders, {labels.Count(l => l),4} cancelled ({100.0 * labels.Count(l => l) / labels.Length:F1}%)");
        }

        // Two models: a linear one and gradient-boosted trees, both on the same features
        ITransformer logistic = Features(ml).Append(Logistic(ml)).Fit(train);
        ITransformer trees = Features(ml).Append(ml.BinaryClassification.Trainers.FastTree(
            labelColumnName: "IsCancelled", featureColumnName: "Features")).Fit(train);

        Console.WriteLine();
        Console.WriteLine($"{"test set, threshold 0.5",-24}{"Accuracy",9}{"AUC",7}{"PR-AUC",8}{"Precision",11}{"Recall",8}{"F1",7}");
        foreach ((string name, ITransformer model) in new[] { ("LogisticRegression", logistic), ("FastTree", trees) })
        {
            CalibratedBinaryClassificationMetrics m =
                ml.BinaryClassification.Evaluate(model.Transform(test), labelColumnName: "IsCancelled");
            Console.WriteLine($"{name,-24}{m.Accuracy,9:F3}{m.AreaUnderRocCurve,7:F3}{m.AreaUnderPrecisionRecallCurve,8:F3}" +
                              $"{m.PositivePrecision,11:F3}{m.PositiveRecall,8:F3}{m.F1Score,7:F3}");
        }
        CalibratedBinaryClassificationMetrics lm =
            ml.BinaryClassification.Evaluate(logistic.Transform(test), labelColumnName: "IsCancelled");
        Console.WriteLine();
        Console.WriteLine(lm.ConfusionMatrix.GetFormattedConfusionTable());

        // Cancellation rate per payment method and stock state, from the training set only
        List<CancelInput> trainRows = [.. ml.Data.CreateEnumerable<CancelInput>(train, reuseRowObject: false)];
        Console.WriteLine();
        foreach (IGrouping<string, CancelInput> g in trainRows
                     .GroupBy(r => r.PaymentMethod)
                     .Concat(trainRows.GroupBy(r => r.HasBackorderItem ? "back-ordered" : "in stock")))
        {
            Console.WriteLine($"{g.Key,-16}{g.Count(),7:N0} orders, cancelled {g.Average(r => r.IsCancelled ? 1.0 : 0.0),6:P1}");
        }

        // Baselines: "nobody cancels" and a hand-written rule
        List<CancelInput> testRows = [.. ml.Data.CreateEnumerable<CancelInput>(test, reuseRowObject: false)];
        Print("rule: COD or back-order", Counts(testRows.Select(r => (r.IsCancelled,
            r.PaymentMethod == "CashOnDelivery" || r.HasBackorderItem))));

        // Threshold sweep on the validation set only
        List<CancelScore> val = Scores(ml, logistic, validation);
        List<string> csv = ["threshold,precision,recall,flagged"];
        Console.WriteLine();
        Console.WriteLine($"{"validation",-11}{"flagged",8}{"TP",5}{"FP",6}{"FN",5}{"Precision",11}{"Recall",8}{"F1",7}{"cost TL",9}");
        (double Threshold, double Cost) best = (0, double.MaxValue);
        for (double t = 0.02; t <= 0.301; t += 0.01)
        {
            (int tp, int fp, int fn, int tn) = Counts(val.Select(s => (s.IsCancelled, s.Probability >= t)));
            double precision = tp + fp == 0 ? 0 : (double)tp / (tp + fp), recall = (double)tp / (tp + fn);
            double cost = Cost(fp, fn);
            csv.Add(string.Create(CultureInfo.InvariantCulture, $"{t:F2},{precision:F4},{recall:F4},{tp + fp}"));
            if (cost < best.Cost) best = (t, cost);
            if (Math.Round(t * 100) % 2 == 0)
            {
                Console.WriteLine($"t = {t,-7:F2}{tp + fp,8}{tp,5}{fp,6}{fn,5}{precision,11:F3}{recall,8:F3}" +
                                  $"{F1(precision, recall),7:F3}{cost,9:N0}");
            }
        }
        Console.WriteLine($"lowest validation cost at t = {best.Threshold:F2}; " +
                          $"cost ratio {FalseAlarmCost} / ({FalseAlarmCost} + {MissedCancelCost}) = {FalseAlarmCost / (FalseAlarmCost + MissedCancelCost):F3}");

        // The chosen threshold, applied once to the test set
        List<CancelScore> scored = Scores(ml, logistic, test);
        (int TP, int FP, int FN, int TN) c = Counts(scored.Select(s => (s.IsCancelled, s.Probability >= best.Threshold)));
        Console.WriteLine();
        Print($"test, LogisticRegression t = {best.Threshold:F2}", c);
        Print("test, flag nobody (t = 1)", Counts(scored.Select(s => (s.IsCancelled, false))));
        Directory.CreateDirectory("out");
        File.WriteAllLines("out/07-threshold.csv", csv);
        File.WriteAllText("out/07-confusion.csv", $"tp,fp,fn,tn,threshold\n{c.TP},{c.FP},{c.FN},{c.TN},{best.Threshold.ToString("F2", CultureInfo.InvariantCulture)}\n");

        // Calibration: does "0.20" mean 20 out of 100?
        Console.WriteLine();
        Console.WriteLine($"{"probability band",-18}{"LR orders",10}{"predicted",10}{"actual",8}{"FT orders",11}{"predicted",10}{"actual",8}");
        List<CancelScore> ft = Scores(ml, trees, test);
        foreach ((double lo, double hi) in new[] { (0.0, 0.03), (0.03, 0.06), (0.06, 0.10), (0.10, 0.15), (0.15, 1.0) })
        {
            List<CancelScore> a = [.. scored.Where(s => s.Probability >= lo && s.Probability < hi)];
            List<CancelScore> b = [.. ft.Where(s => s.Probability >= lo && s.Probability < hi)];
            Console.WriteLine($"{lo:F2} - {hi:F2}       {a.Count,10:N0}{Mean(a, s => s.Probability),10:P1}{Mean(a, s => s.IsCancelled ? 1 : 0),8:P1}" +
                              $"{b.Count,11:N0}{Mean(b, s => s.Probability),10:P1}{Mean(b, s => s.IsCancelled ? 1 : 0),8:P1}");
        }
    }

    /// <summary>Assumed costs (TL): an SMS confirmation for a false alarm, wasted picking and supplier order for a miss.</summary>
    private const double FalseAlarmCost = 3, MissedCancelCost = 40;

    private static double Cost(int falsePositives, int falseNegatives) =>
        falsePositives * FalseAlarmCost + falseNegatives * MissedCancelCost;

    public static List<CancelScore> Scores(MLContext ml, ITransformer model, IDataView data) =>
        [.. ml.Data.CreateEnumerable<CancelScore>(model.Transform(data), reuseRowObject: false)];

    public static (int TP, int FP, int FN, int TN) Counts(IEnumerable<(bool Actual, bool Flagged)> rows)
    {
        int tp = 0, fp = 0, fn = 0, tn = 0;
        foreach ((bool actual, bool flagged) in rows)
        {
            if (actual && flagged) tp++;
            else if (!actual && flagged) fp++;
            else if (actual) fn++;
            else tn++;
        }
        return (tp, fp, fn, tn);
    }

    private static void Print(string name, (int TP, int FP, int FN, int TN) c)
    {
        double precision = c.TP + c.FP == 0 ? 0 : (double)c.TP / (c.TP + c.FP), recall = (double)c.TP / (c.TP + c.FN);
        double accuracy = (double)(c.TP + c.TN) / (c.TP + c.FP + c.FN + c.TN);
        Console.WriteLine($"{name}: TP {c.TP}, FP {c.FP}, FN {c.FN}, TN {c.TN:N0} | accuracy {accuracy:F3}, " +
                          $"precision {precision:F3}, recall {recall:F3}, cost {Cost(c.FP, c.FN):N0} TL");
    }

    private static double F1(double p, double r) => p + r == 0 ? 0 : 2 * p * r / (p + r);

    private static double Mean(List<CancelScore> rows, Func<CancelScore, double> f) => rows.Count == 0 ? 0 : rows.Average(f);

    public static void Topics()
    {
        MLContext ml = new(seed: 1);
        IDataView data = ml.Data.LoadFromTextFile<ReviewInput>("data/reviews.csv",
            new TextLoader.Options { HasHeader = true, Separators = [','], AllowQuoting = true });
        var split = ml.Data.TrainTestSplit(data, testFraction: 0.2, seed: 7);

        var pipeline = ml.Transforms.Conversion.MapValueToKey("Label", "Topic")
            .Append(ml.Transforms.Text.FeaturizeText("Features", "Text"))
            .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options
            {
                LabelColumnName = "Label", FeatureColumnName = "Features", NumberOfThreads = 1,
            }))
            .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedTopic", "PredictedLabel"));
        ITransformer model = pipeline.Fit(split.TrainSet);
        IDataView scored = model.Transform(split.TestSet);
        MulticlassClassificationMetrics m = ml.MulticlassClassification.Evaluate(scored);

        string[] topics = [.. split.TrainSet.GetColumn<string>("Topic")];
        string majority = topics.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key;
        string[] testTopics = [.. split.TestSet.GetColumn<string>("Topic")];
        Console.WriteLine($"test reviews {testTopics.Length:N0}; always '{majority}': accuracy {testTopics.Count(t => t == majority) / (double)testTopics.Length:F3}");
        Console.WriteLine($"MicroAccuracy {m.MicroAccuracy:F3}  MacroAccuracy {m.MacroAccuracy:F3}  LogLoss {m.LogLoss:F3}  LogLossReduction {m.LogLossReduction:F3}");
        Console.WriteLine();
        Console.WriteLine(m.ConfusionMatrix.GetFormattedConfusionTable());

        List<TopicPrediction> rows = [.. ml.Data.CreateEnumerable<TopicPrediction>(scored, reuseRowObject: false)];
        Console.WriteLine("wrong predictions (first five):");
        foreach (TopicPrediction p in rows.Where(r => r.Topic != r.PredictedTopic).Take(5))
        {
            Console.WriteLine($"  label {p.Topic,-12} predicted {p.PredictedTopic,-12} max score {p.Score.Max():F2}  \"{p.Text}\"");
        }
    }
}
