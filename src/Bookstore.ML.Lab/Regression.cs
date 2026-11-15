using System.Globalization;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Bookstore.ML.Lab;

/// <summary>Post 06: how regression learns, which algorithm to pick and how to read MAE, RMSE and R².</summary>
public static class Regression
{
    /// <summary>Linear regression with plain gradient descent: days = w * (DistanceKm / 1000) + b.</summary>
    public static void GradientDescent()
    {
        (double X, double Y)[] points = [.. OrderRow.Load("data/orders_clean.csv")
            .Where(o => o.DeliveryDays is not null && o.ShippingMethod == "Standard" && !o.HasBackorderItem)
            .Select(o => (o.DistanceKm / 1000.0, o.DeliveryDays!.Value))];

        foreach (double learningRate in new[] { 0.1, 0.5, 1.0 })
        {
            double w = 0, b = 0;
            Console.WriteLine($"learning rate {learningRate}:");
            for (int epoch = 0; epoch <= 300; epoch++)
            {
                double gradW = 0, gradB = 0, loss = 0;
                foreach ((double x, double y) in points)
                {
                    double error = w * x + b - y;      // prediction minus truth
                    loss += error * error;
                    gradW += 2 * error * x;
                    gradB += 2 * error;
                }
                loss /= points.Length;
                if (epoch is 0 or 1 or 2 or 5 or 20 or 100 or 300)
                {
                    Console.WriteLine($"  epoch {epoch,3}: MSE {loss,11:G6}   w {w,10:G5}   b {b,10:G5}");
                }
                w -= learningRate * gradW / points.Length;
                b -= learningRate * gradB / points.Length;
            }
        }

        // The exact least-squares answer for one feature, for comparison
        double mx = points.Average(p => p.X), my = points.Average(p => p.Y);
        double slope = points.Sum(p => (p.X - mx) * (p.Y - my)) / points.Sum(p => (p.X - mx) * (p.X - mx));
        Console.WriteLine($"closed form: w {slope:F4}   b {my - slope * mx:F4}   ({points.Length:N0} orders)");
    }

    public static void Compare()
    {
        MLContext ml = new(seed: 1);
        IDataView data = ml.Data.FilterRowsByMissingValues(FeatureLab.Load(), "DeliveryDays");
        DataOperationsCatalog.TrainTestData split =
            ml.Data.TrainTestSplit(data, testFraction: 0.2, samplingKeyColumnName: "CustomerId", seed: 7);
        IDataView train = ml.Data.Cache(split.TrainSet);

        Dictionary<string, IEstimator<ITransformer>> trainers = new()
        {
            ["SDCA"] = ml.Regression.Trainers.Sdca(new SdcaRegressionTrainer.Options
            {
                LabelColumnName = "DeliveryDays", FeatureColumnName = "Features", NumberOfThreads = 1,
            }),
            ["FastTree"] = ml.Regression.Trainers.FastTree(labelColumnName: "DeliveryDays", featureColumnName: "Features"),
            ["FastForest"] = ml.Regression.Trainers.FastForest(labelColumnName: "DeliveryDays", featureColumnName: "Features"),
            ["LightGBM"] = ml.Regression.Trainers.LightGbm(labelColumnName: "DeliveryDays", featureColumnName: "Features"),
        };

        Console.WriteLine($"{"trainer",-11}{"MAE",8}{"RMSE",8}{"R2",8}");
        Dictionary<string, ITransformer> models = [];
        foreach ((string name, IEstimator<ITransformer> trainer) in trainers)
        {
            ITransformer model = FeatureLab.Full(trainer).Fit(train);
            RegressionMetrics m = ml.Regression.Evaluate(model.Transform(split.TestSet), labelColumnName: "DeliveryDays");
            Console.WriteLine($"{name,-11}{m.MeanAbsoluteError,8:F3}{m.RootMeanSquaredError,8:F3}{m.RSquared,8:F3}");
            models[name] = model;
        }

        Residuals(ml, models, split.TestSet);
        Outliers(ml, models["LightGBM"], split.TestSet);
        Extrapolation(ml, data);
    }

    private sealed class Scored
    {
        public float DeliveryDays { get; set; }
        public float Score { get; set; }
        public string Carrier { get; set; } = "";
        public string ShippingMethod { get; set; } = "";
        public bool HasBackorderItem { get; set; }
        public bool IsCampaign { get; set; }
        public float HoursUntilProcessing { get; set; }
        public float ItemCount { get; set; }
    }

    /// <summary>Mean of (prediction - truth) per group: a non-zero mean is a systematic error.</summary>
    private static void Residuals(MLContext ml, Dictionary<string, ITransformer> models, IDataView test)
    {
        (string Group, Func<Scored, bool> Filter)[] groups =
        [
            ("all test orders", _ => true),
            ("Express", s => s.ShippingMethod == "Express"),
            ("back-ordered", s => s.HasBackorderItem),
            ("campaign week", s => s.IsCampaign),
            ("campaign + back-ordered", s => s.IsCampaign && s.HasBackorderItem),
            ("waits > 40 h (weekend)", s => s.HoursUntilProcessing > 40),
            ("delivered in > 15 days", s => s.DeliveryDays > 15),
        ];
        List<string> csv = ["group,model,bias,count"];
        Console.WriteLine();
        Console.WriteLine($"{"mean error (days)",-26}{"SDCA",8}{"LightGBM",10}{"orders",8}");
        Dictionary<string, List<Scored>> scored = models
            .Where(m => m.Key is "SDCA" or "LightGBM")
            .ToDictionary(m => m.Key, m => ml.Data.CreateEnumerable<Scored>(m.Value.Transform(test), false).ToList());
        foreach ((string group, Func<Scored, bool> filter) in groups)
        {
            List<Scored> a = [.. scored["SDCA"].Where(filter)], b = [.. scored["LightGBM"].Where(filter)];
            double ba = a.Average(s => s.Score - s.DeliveryDays), bb = b.Average(s => s.Score - s.DeliveryDays);
            Console.WriteLine($"{group,-26}{ba,8:F2}{bb,10:F2}{a.Count,8}");
            csv.Add(string.Create(CultureInfo.InvariantCulture, $"{group},SDCA,{ba:F3},{a.Count}"));
            csv.Add(string.Create(CultureInfo.InvariantCulture, $"{group},LightGBM,{bb:F3},{b.Count}"));
        }
        Directory.CreateDirectory("out");
        File.WriteAllLines("out/06-residuals.csv", csv);
        File.WriteAllLines("out/06-predictions.csv", ["actual,lightgbm", .. scored["LightGBM"].Select(x =>
            string.Create(CultureInfo.InvariantCulture, $"{x.DeliveryDays:F2},{x.Score:F3}"))]);
    }

    /// <summary>A handful of lost parcels and what they do to each metric.</summary>
    private static void Outliers(MLContext ml, ITransformer model, IDataView test)
    {
        IDataView normal = ml.Data.FilterRowsByColumn(test, "DeliveryDays", upperBound: 15);
        RegressionMetrics all = ml.Regression.Evaluate(model.Transform(test), labelColumnName: "DeliveryDays");
        RegressionMetrics clean = ml.Regression.Evaluate(model.Transform(normal), labelColumnName: "DeliveryDays");
        long lost = test.GetColumn<float>("DeliveryDays").Count(d => d > 15);
        Console.WriteLine();
        Console.WriteLine($"LightGBM, all test rows       : MAE {all.MeanAbsoluteError:F3}  RMSE {all.RootMeanSquaredError:F3}  R2 {all.RSquared:F3}");
        Console.WriteLine($"without {lost} rows over 15 days : MAE {clean.MeanAbsoluteError:F3}  RMSE {clean.RootMeanSquaredError:F3}  R2 {clean.RSquared:F3}");

        foreach (string method in new[] { "Express", "Standard" })
        {
            IDataView subset = ml.Data.FilterByCustomPredicate<DeliveryInput>(normal, o => o.ShippingMethod != method);
            RegressionMetrics m = ml.Regression.Evaluate(model.Transform(subset), labelColumnName: "DeliveryDays");
            Console.WriteLine($"  only {method,-8}: MAE {m.MeanAbsoluteError:F3}  RMSE {m.RootMeanSquaredError:F3}  R2 {m.RSquared:F3}");
        }
    }

    /// <summary>Train on individual orders only (at most 12 books), then ask about institutional orders.</summary>
    private static void Extrapolation(MLContext ml, IDataView data)
    {
        IDataView individual = ml.Data.FilterRowsByColumn(data, "ItemCount", upperBound: 13);
        List<DeliveryInput> institutional = [.. ml.Data.CreateEnumerable<DeliveryInput>(
            ml.Data.FilterRowsByColumn(data, "ItemCount", lowerBound: 20), false)];

        ITransformer sdca = FeatureLab.Full(ml.Regression.Trainers.Sdca(new SdcaRegressionTrainer.Options
        {
            LabelColumnName = "DeliveryDays", FeatureColumnName = "Features", NumberOfThreads = 1,
        })).Fit(individual);
        ITransformer gbm = FeatureLab.Full(ml.Regression.Trainers.LightGbm(labelColumnName: "DeliveryDays",
            featureColumnName: "Features")).Fit(individual);
        var e1 = ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(sdca);
        var e2 = ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(gbm);

        Console.WriteLine();
        Console.WriteLine($"trained on ItemCount <= 12; institutional orders in the data: {institutional.Count}, " +
                          $"mean {institutional.Average(o => o.DeliveryDays):F2} days");
        double maeSdca = institutional.Average(o => Math.Abs(e1.Predict(o).PredictedDays - o.DeliveryDays));
        double maeGbm = institutional.Average(o => Math.Abs(e2.Predict(o).PredictedDays - o.DeliveryDays));
        Console.WriteLine($"  MAE on institutional orders: SDCA {maeSdca:F3}, LightGBM {maeGbm:F3}");

        // The same question for distance: train below 1,000 km, ask about Van (1,220 km from ANK-1)
        IDataView near = ml.Data.FilterRowsByColumn(data, "DistanceKm", upperBound: 1000);
        List<DeliveryInput> far = [.. ml.Data.CreateEnumerable<DeliveryInput>(
            ml.Data.FilterRowsByColumn(data, "DistanceKm", lowerBound: 1000), false)];
        ITransformer sdcaNear = FeatureLab.Full(ml.Regression.Trainers.Sdca(new SdcaRegressionTrainer.Options
        {
            LabelColumnName = "DeliveryDays", FeatureColumnName = "Features", NumberOfThreads = 1,
        })).Fit(near);
        ITransformer gbmNear = FeatureLab.Full(ml.Regression.Trainers.LightGbm(labelColumnName: "DeliveryDays",
            featureColumnName: "Features")).Fit(near);
        var n1 = ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(sdcaNear);
        var n2 = ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(gbmNear);
        Console.WriteLine($"trained on DistanceKm < 1000; {far.Count} orders travel farther, mean {far.Average(o => o.DeliveryDays):F2} days");
        DeliveryInput order = far.First(o => !o.HasBackorderItem && o.ShippingMethod == "Standard" && o.CustomerNote == "");
        foreach (int km in new[] { 300, 600, 900, 1200, 1600 })
        {
            order.DistanceKm = km;
            Console.WriteLine($"  {km,5} km: SDCA {n1.Predict(order).PredictedDays,5:F2}   LightGBM {n2.Predict(order).PredictedDays,5:F2}");
        }
        Console.WriteLine($"  MAE on the far orders: SDCA {far.Average(o => Math.Abs(n1.Predict(o).PredictedDays - o.DeliveryDays)):F3}, " +
                          $"LightGBM {far.Average(o => Math.Abs(n2.Predict(o).PredictedDays - o.DeliveryDays)):F3}");
    }
}
