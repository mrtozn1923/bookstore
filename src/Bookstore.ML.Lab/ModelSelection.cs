using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Calibrators;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Trainers.FastTree;

namespace Bookstore.ML.Lab;

/// <summary>Post 09: hyperparameter search, AutoML, permutation feature importance and a fairness check.</summary>
public static class ModelSelection
{
    private sealed record Split(IDataView Train, IDataView Validation, IDataView Test, IDataView TrainAndValidation);

    /// <summary>The same 60/20/20 customer-grouped split as post 07.</summary>
    private static Split Load(MLContext ml)
    {
        IDataView all = ml.Data.LoadFromEnumerable(Classification.LoadOrders());
        var first = ml.Data.TrainTestSplit(all, testFraction: 0.2, samplingKeyColumnName: "CustomerId", seed: 7);
        var second = ml.Data.TrainTestSplit(first.TrainSet, testFraction: 0.25, samplingKeyColumnName: "CustomerId", seed: 11);
        return new Split(ml.Data.Cache(second.TrainSet), second.TestSet, first.TestSet, ml.Data.Cache(first.TrainSet));
    }

    private static (double Mean, double Std) CvAuc(MLContext ml, IDataView data, IEstimator<ITransformer> pipeline)
    {
        double[] auc = [.. ml.BinaryClassification.CrossValidateNonCalibrated(data, pipeline, numberOfFolds: 5,
            labelColumnName: "IsCancelled", samplingKeyColumnName: "CustomerId", seed: 3).Select(f => f.Metrics.AreaUnderRocCurve)];
        double mean = auc.Average();
        return (mean, Math.Sqrt(auc.Average(a => (a - mean) * (a - mean))));
    }

    private static IEstimator<ITransformer> Tree(MLContext ml, int leaves, int minLeaf, int trees, double rate) =>
        Classification.Features(ml).Append(ml.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options
        {
            LabelColumnName = "IsCancelled", FeatureColumnName = "Features",
            NumberOfLeaves = leaves, MinimumExampleCountPerLeaf = minLeaf, NumberOfTrees = trees, LearningRate = rate,
        }));

    public static void Tune()
    {
        MLContext ml = new(seed: 1);
        Split s = Load(ml);

        // The parameters of a logistic regression: one learned weight per feature slot
        var logistic = Classification.Features(ml).Append(ml.BinaryClassification.Trainers.LbfgsLogisticRegression(
            new LbfgsLogisticRegressionBinaryTrainer.Options
            {
                LabelColumnName = "IsCancelled", FeatureColumnName = "Features", NumberOfThreads = 1,
            })).Fit(s.Train);
        VBuffer<ReadOnlyMemory<char>> names = default;
        logistic.Transform(s.Train).Schema["Features"].GetSlotNames(ref names);
        float[] weights = [.. logistic.LastTransformer.Model.SubModel.Weights];
        Console.WriteLine("logistic regression weights (largest first):");
        foreach ((string name, float w) in names.DenseValues().Select(n => n.ToString()).Zip(weights)
                     .OrderByDescending(x => Math.Abs(x.Second)).Take(6))
        {
            Console.WriteLine($"  {name,-28}{w,8:F3}");
        }

        (double lrMean, double lrStd) = CvAuc(ml, s.TrainAndValidation, Classification.Features(ml).Append(Classification.Logistic(ml)));
        Console.WriteLine();
        Console.WriteLine($"LogisticRegression, 5-fold CV AUC {lrMean:F3} ± {lrStd:F3}");

        // Grid search: every combination, each one scored with 5-fold cross-validation
        List<(string Name, double Mean, double Std)> results = [];
        Console.WriteLine($"{"FastTree grid (leaves, min leaf, rate)",-42}{"CV AUC",8}{"±",7}");
        foreach (int leaves in new[] { 2, 4, 16, 64 })
        foreach (int minLeaf in new[] { 10, 200 })
        foreach (double rate in new[] { 0.05, 0.2 })
        {
            (double mean, double std) = CvAuc(ml, s.TrainAndValidation, Tree(ml, leaves, minLeaf, 100, rate));
            string name = $"leaves {leaves,2}, min leaf {minLeaf,3}, rate {rate:F2}";
            results.Add((name, mean, std));
            Console.WriteLine($"{name,-42}{mean,8:F3}{std,7:F3}");
        }

        // Random search: the same budget spent on random points of a wider space
        Random rnd = new(42);
        Console.WriteLine($"{"FastTree random (leaves, min leaf, trees, rate)",-50}{"CV AUC",8}");
        for (int i = 0; i < 8; i++)
        {
            int leaves = 2 << rnd.Next(0, 6), minLeaf = rnd.Next(5, 400), trees = rnd.Next(20, 300);
            double rate = Math.Round(Math.Pow(10, -2.5 + 2 * rnd.NextDouble()), 3);
            (double mean, _) = CvAuc(ml, s.TrainAndValidation, Tree(ml, leaves, minLeaf, trees, rate));
            Console.WriteLine($"{$"leaves {leaves,2}, min leaf {minLeaf,3}, trees {trees,3}, rate {rate:F3}",-50}{mean,8:F3}");
        }

        // The winner of the grid, trained once on train + validation and measured once on the test set
        (string bestName, double bestMean, _) = results.MaxBy(r => r.Mean);
        string[] p = bestName.Split(',', StringSplitOptions.TrimEntries);
        int bl = int.Parse(p[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
        int bm = int.Parse(p[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[2]);
        double br = double.Parse(p[2].Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
        ITransformer best = Tree(ml, bl, bm, 100, br).Fit(s.TrainAndValidation);
        ITransformer lr = Classification.Features(ml).Append(Classification.Logistic(ml)).Fit(s.TrainAndValidation);
        Console.WriteLine();
        Console.WriteLine($"best grid point: {bestName} (CV AUC {bestMean:F3})");
        Console.WriteLine($"test AUC: FastTree {Auc(ml, best, s.Test):F3}, LogisticRegression {Auc(ml, lr, s.Test):F3}");
    }

    private static double Auc(MLContext ml, ITransformer model, IDataView data) =>
        ml.BinaryClassification.EvaluateNonCalibrated(model.Transform(data), labelColumnName: "IsCancelled").AreaUnderRocCurve;

    /// <summary>Prints every trial: which pipeline AutoML tried, its validation AUC and how long it took.</summary>
    private sealed class TrialPrinter(SweepablePipeline pipeline) : IMonitor
    {
        public void ReportCompletedTrial(TrialResult result) =>
            Console.WriteLine($"  trial {result.TrialSettings.TrialId,2}  AUC {result.Metric:F4}  {result.DurationInMilliseconds / 1000,5:F1} s  " +
                              pipeline.ToString(result.TrialSettings.Parameter));

        public void ReportBestTrial(TrialResult result) { }
        public void ReportFailTrial(TrialSettings settings, Exception? exception = null) =>
            Console.WriteLine($"  trial {settings.TrialId,2}  failed: {exception?.GetType().Name}");
        public void ReportRunningTrial(TrialSettings settings) { }
    }

    public static async Task AutoMlAsync(bool withLeak)
    {
        MLContext ml = new(seed: 1);
        Split s = Load(ml);
        ColumnInformation columns = new() { LabelColumnName = "IsCancelled" };
        foreach (string c in new[] { "PaymentMethod", "ShippingMethod", "Warehouse" }) columns.CategoricalColumnNames.Add(c);
        foreach (string c in new[] { "ItemCount", "OrderTotal", "DiscountRate", "PreviousOrders" }) columns.NumericColumnNames.Add(c);
        foreach (string c in new[] { "OrderId", "CustomerId", "OrderedAtUtc", "Age", "District" }) columns.IgnoredColumnNames.Add(c);
        // TrackingEventCount only exists after shipping: a cancelled order always has 0
        if (withLeak) columns.NumericColumnNames.Add("TrackingEventCount");
        else columns.IgnoredColumnNames.Add("TrackingEventCount");

        SweepablePipeline pipeline = ml.Auto().Featurizer(s.Train, columns)
            .Append(ml.Auto().BinaryClassification(labelColumnName: "IsCancelled"));
        AutoMLExperiment experiment = ml.Auto().CreateExperiment()
            .SetPipeline(pipeline)
            .SetDataset(s.Train, s.Validation)
            .SetBinaryClassificationMetric(BinaryClassificationMetric.AreaUnderRocCurve, labelColumn: "IsCancelled")
            .SetTrainingTimeInSeconds(60)
            .SetMaxModelToExplore(withLeak ? 4 : 20);
        experiment.SetMonitor(new TrialPrinter(pipeline));

        Console.WriteLine(withLeak ? "AutoML with TrackingEventCount:" : "AutoML, 20 trials at most, 60 s at most:");
        TrialResult best = await experiment.RunAsync();
        Console.WriteLine($"best trial {best.TrialSettings.TrialId}: validation AUC {best.Metric:F4}, " +
                          $"test AUC {Auc(ml, best.Model, s.Test):F4}");
    }

    public static void Explain()
    {
        MLContext ml = new(seed: 1);
        Split s = Load(ml);
        var model = Classification.Features(ml).Append(ml.BinaryClassification.Trainers.LbfgsLogisticRegression(
            new LbfgsLogisticRegressionBinaryTrainer.Options
            {
                LabelColumnName = "IsCancelled", FeatureColumnName = "Features", NumberOfThreads = 1,
            })).Fit(s.TrainAndValidation);
        IDataView test = model.Transform(s.Test);

        // Shuffle one feature slot at a time and measure how much the test AUC drops
        var pfi = ml.BinaryClassification.PermutationFeatureImportance(model.LastTransformer, test,
            labelColumnName: "IsCancelled", permutationCount: 5);
        VBuffer<ReadOnlyMemory<char>> names = default;
        test.Schema["Features"].GetSlotNames(ref names);
        Console.WriteLine($"{"feature slot",-28}{"AUC drop",10}{"±",8}");
        foreach ((string name, BinaryClassificationMetricsStatistics m) in names.DenseValues().Select(n => n.ToString())
                     .Zip(pfi).OrderBy(x => x.Second.AreaUnderRocCurve.Mean).Take(8))
        {
            Console.WriteLine($"{name,-28}{-m.AreaUnderRocCurve.Mean,10:F4}{m.AreaUnderRocCurve.StandardError,8:F4}");
        }
    }

    public static void Fairness()
    {
        MLContext ml = new(seed: 1);
        Split s = Load(ml);
        (string Name, IEstimator<ITransformer> Pipeline)[] variants =
        [
            ("order features", Classification.Features(ml).Append(Classification.Logistic(ml))),
            ("+ Age", Classification.Features(ml, ["Age"]).Append(Classification.Logistic(ml))),
            ("+ District (one-hot)", ml.Transforms.Categorical.OneHotEncoding("DistrictKey", "District")
                .Append(Classification.Features(ml, ["DistrictKey"])).Append(Classification.Logistic(ml))),
        ];
        string[] bands = ["under 30", "30-49", "50+"];
        static string Band(float age) => age < 30 ? "under 30" : age < 50 ? "30-49" : "50+";

        float[] ages = [.. s.Test.GetColumn<float>("Age")];
        bool[] actual = [.. s.Test.GetColumn<bool>("IsCancelled")];
        Console.WriteLine($"{"",-22}{"test AUC",9}" + string.Concat(bands.Select(b => $"{"flagged " + b,18}")));
        Console.WriteLine($"{"actual cancel rate",-22}{"",9}" + string.Concat(bands.Select(b =>
            $"{Enumerable.Range(0, ages.Length).Where(i => Band(ages[i]) == b).Average(i => actual[i] ? 1.0 : 0.0),18:P1}")));
        foreach ((string name, IEstimator<ITransformer> pipeline) in variants)
        {
            ITransformer model = pipeline.Fit(s.TrainAndValidation);
            IDataView scored = model.Transform(s.Test);
            float[] p = [.. scored.GetColumn<float>("Probability")];
            double auc = ml.BinaryClassification.Evaluate(scored, labelColumnName: "IsCancelled").AreaUnderRocCurve;
            Console.WriteLine($"{name,-22}{auc,9:F3}" + string.Concat(bands.Select(b =>
                $"{Enumerable.Range(0, ages.Length).Where(i => Band(ages[i]) == b).Average(i => p[i] >= 0.06 ? 1.0 : 0.0),18:P1}")));
        }
    }
}
