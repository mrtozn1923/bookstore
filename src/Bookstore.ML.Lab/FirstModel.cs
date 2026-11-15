using System.Diagnostics;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Bookstore.ML.Lab;

/// <summary>Post 04: the first ML.NET model, step by step.</summary>
public static class FirstModel
{
    public const string ModelPath = "models/delivery-sdca.zip";

    public static void Run()
    {
        MLContext ml = new(seed: 1);

        // 1) Loading is lazy: nothing is read until someone iterates over the rows.
        //    MissingRealsAsNaNs: an empty number becomes NaN instead of 0 (the default).
        IDataView data = ml.Data.LoadFromTextFile<DeliveryInput>("data/orders_clean.csv", new TextLoader.Options
        {
            HasHeader = true,
            Separators = [','],
            MissingRealsAsNaNs = true,
        });
        Console.WriteLine($"row count known before reading? {data.GetRowCount()?.ToString() ?? "no (null)"}");
        foreach (DataViewSchema.Column column in data.Schema.Take(10))
        {
            Console.WriteLine($"  {column.Index,2} {column.Name,-16} {column.Type}");
        }

        // 2) Cancelled orders have an empty label (NaN): drop those rows
        IDataView delivered = ml.Data.FilterRowsByMissingValues(data, "DeliveryDays");
        Console.WriteLine($"rows with a label: {delivered.GetColumn<float>("DeliveryDays").Count():N0}");
        DataOperationsCatalog.TrainTestData split = ml.Data.TrainTestSplit(
            delivered, testFraction: 0.2, samplingKeyColumnName: "CustomerId", seed: 7);

        // 3) The pipeline: a chain of estimators, nothing is learned yet
        var pipeline = ml.Transforms.Categorical.OneHotEncoding(
                [new("CarrierKey", "Carrier"), new("MethodKey", "ShippingMethod"), new("WarehouseKey", "Warehouse")])
            .Append(ml.Transforms.Conversion.ConvertType(
                [new("Campaign", "IsCampaign"), new("Backorder", "HasBackorderItem")], DataKind.Single))
            .Append(ml.Transforms.Concatenate("Features",
                "CarrierKey", "MethodKey", "WarehouseKey", "Campaign", "Backorder", "DistanceKm", "ItemCount"))
            .Append(ml.Transforms.NormalizeMinMax("Features"))
            .Append(ml.Regression.Trainers.Sdca(new SdcaRegressionTrainer.Options
            {
                LabelColumnName = "DeliveryDays",
                FeatureColumnName = "Features",
                NumberOfThreads = 1, // the same result on every run
            }));
        Console.WriteLine($"pipeline: {pipeline.GetType().Name}");

        // 4) Fit learns the one-hot vocabularies, the min/max values and the weights
        Stopwatch sw = Stopwatch.StartNew();
        ITransformer model = pipeline.Fit(split.TrainSet);
        Console.WriteLine($"model:    {model.GetType().Name}, trained in {sw.ElapsedMilliseconds} ms");

        IDataView scored = model.Transform(split.TestSet);
        RegressionMetrics metrics = ml.Regression.Evaluate(scored, labelColumnName: "DeliveryDays");
        Console.WriteLine($"test MAE {metrics.MeanAbsoluteError:F3} days, RMSE {metrics.RootMeanSquaredError:F3}, " +
                          $"R2 {metrics.RSquared:F3}");

        // 5) One order at a time
        PredictionEngine<DeliveryInput, DeliveryPrediction> engine =
            ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(model);
        DeliveryInput toVan = new()
        {
            Carrier = "CarrierC", ShippingMethod = "Standard", Warehouse = "ANK-1",
            DistanceKm = 1220, ItemCount = 2,
        };
        DeliveryInput inIstanbul = new()
        {
            Carrier = "CarrierA", ShippingMethod = "Express", Warehouse = "IST-1",
            DistanceKm = 25, ItemCount = 1,
        };
        Console.WriteLine($"Ankara -> Van, CarrierC, 2 books       : {engine.Predict(toVan).PredictedDays:F2} days");
        Console.WriteLine($"Istanbul, Express, 1 book               : {engine.Predict(inIstanbul).PredictedDays:F2} days");
        Console.WriteLine($"same, but one item is back-ordered      : " +
                          $"{engine.Predict(new DeliveryInput { Carrier = "CarrierA", ShippingMethod = "Express", Warehouse = "IST-1", DistanceKm = 25, ItemCount = 1, HasBackorderItem = true }).PredictedDays:F2} days");

        // 6) Save the whole chain together with the input schema
        Directory.CreateDirectory("models");
        ml.Model.Save(model, split.TrainSet.Schema, ModelPath);
        Console.WriteLine($"saved {ModelPath} ({new FileInfo(ModelPath).Length / 1024.0:F1} KB)");

        MLContext other = new();
        ITransformer loaded = other.Model.Load(ModelPath, out DataViewSchema inputSchema);
        PredictionEngine<DeliveryInput, DeliveryPrediction> engine2 =
            other.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(loaded);
        Console.WriteLine($"loaded model, Ankara -> Van              : {engine2.Predict(toVan).PredictedDays:F2} days " +
                          $"(input schema has {inputSchema.Count} columns)");
    }
}
