using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Text;

namespace Bookstore.ML.Lab;

/// <summary>Post 05: add features one group at a time and measure each step with cross-validation.</summary>
public static class FeatureLab
{
    private static MLContext Ml = new(seed: 1);

    private static IEstimator<ITransformer> Sdca() => Ml.Regression.Trainers.Sdca(new SdcaRegressionTrainer.Options
    {
        LabelColumnName = "DeliveryDays", FeatureColumnName = "Features", NumberOfThreads = 1,
    });

    private static IEstimator<ITransformer> Tree() =>
        Ml.Regression.Trainers.FastTree(labelColumnName: "DeliveryDays", featureColumnName: "Features");

    /// <summary>The post 04 columns: one-hot categories, booleans as 0/1, two numbers.</summary>
    private static IEstimator<ITransformer> Base() =>
        Ml.Transforms.Categorical.OneHotEncoding(
                [new("CarrierKey", "Carrier"), new("MethodKey", "ShippingMethod"), new("WarehouseKey", "Warehouse")])
            .Append(Ml.Transforms.Conversion.ConvertType(
                [new("Campaign", "IsCampaign"), new("Backorder", "HasBackorderItem")], DataKind.Single));

    private static readonly string[] BaseColumns =
        ["CarrierKey", "MethodKey", "WarehouseKey", "Campaign", "Backorder", "DistanceKm", "ItemCount"];

    public static IEstimator<ITransformer> OrderTime() =>
        Ml.Transforms.CustomMapping<OrderTimeInput, OrderTimeFeatures>(OrderTimeMappingFactory.Map, "OrderTime")
            .Append(Ml.Transforms.Categorical.OneHotEncoding("LocalDayKey", "LocalDay"));

    public static IEstimator<ITransformer> Weight() =>
        Ml.Transforms.IndicateMissingValues("WeightMissing", "TotalWeightKg")
            .Append(Ml.Transforms.Conversion.ConvertType("WeightMissing", outputKind: DataKind.Single))
            .Append(Ml.Transforms.ReplaceMissingValues("Weight", "TotalWeightKg",
                MissingValueReplacingEstimator.ReplacementMode.Mean));

    public static IEstimator<ITransformer> Note() =>
        Ml.Transforms.Text.FeaturizeText("NoteFeatures", "CustomerNote");

    public static IEstimator<ITransformer> Full(IEstimator<ITransformer> trainer) =>
        Base()
            .Append(Ml.Transforms.Categorical.OneHotEncoding([new("CityKey", "City"), new("DistrictKey", "District")]))
            .Append(OrderTime())
            .Append(Note())
            .Append(Weight())
            .Append(Ml.Transforms.Concatenate("Features", [.. BaseColumns, "CityKey", "DistrictKey", "LocalDayKey", "LocalHour",
                "HoursUntilProcessing", "NoteFeatures", "Weight", "WeightMissing"]))
            .Append(Ml.Transforms.NormalizeMinMax("Features"))
            .Append(trainer);

    public static void Run()
    {
        IDataView data = Ml.Data.FilterRowsByMissingValues(
            Load(),
            "DeliveryDays");
        data = Ml.Data.Cache(data);

        // Every step gets a fresh MLContext with the same seed, so the steps differ only in their features.
        void Step(string name, Func<IEstimator<ITransformer>> features, string[] columns, string normalize = "MinMax", bool tree = true)
        {
            Ml = new MLContext(seed: 1);
            IEstimator<ITransformer> f = features().Append(Ml.Transforms.Concatenate("Features", columns));
            if (normalize == "MinMax") f = f.Append(Ml.Transforms.NormalizeMinMax("Features"));
            if (normalize == "MeanVariance") f = f.Append(Ml.Transforms.NormalizeMeanVariance("Features"));
            string sdca = Cv(data, f.Append(Sdca())).ToString("F3");
            string ft = tree ? Cv(data, f.Append(Tree())).ToString("F3") : "  -  ";
            Console.WriteLine($"{name,-44} SDCA {sdca}   FastTree {ft}");
        }

        string[] city = [.. BaseColumns, "CityKey"];
        string[] district = [.. city, "DistrictKey"];
        string[] time = [.. district, "LocalDayKey", "LocalHour"];
        string[] processing = [.. time, "HoursUntilProcessing"];
        string[] note = [.. processing, "NoteFeatures"];

        IEstimator<ITransformer> City() => Base().Append(Ml.Transforms.Categorical.OneHotEncoding("CityKey", "City"));
        IEstimator<ITransformer> District() => City().Append(Ml.Transforms.Categorical.OneHotEncoding("DistrictKey", "District"));

        Step("04 columns, no normalization", Base, BaseColumns, normalize: "none");
        Step("04 columns, NormalizeMinMax", Base, BaseColumns);
        Step("04 columns, NormalizeMeanVariance", Base, BaseColumns, normalize: "MeanVariance", tree: false);
        Step("04 columns + OrderTotal, no normalization", Base, [.. BaseColumns, "OrderTotal"], normalize: "none");
        Step("04 columns + OrderTotal, NormalizeMinMax", Base, [.. BaseColumns, "OrderTotal"]);
        Step("+ City (one-hot, 16)", City, city);
        Step("+ District (one-hot)", District, district);
        Step("+ District (hash, 32 slots)", () => City().Append(
            Ml.Transforms.Categorical.OneHotHashEncoding("DistrictKey", "District", numberOfBits: 5)), district);
        Step("+ LocalDay, LocalHour", () => District().Append(OrderTime()), time);
        Step("+ HoursUntilProcessing", () => District().Append(OrderTime()), processing);
        Step("+ CustomerNote (FeaturizeText)", () => District().Append(OrderTime()).Append(Note()), note);
        Step("+ TotalWeightKg (mean + missing flag)", () => District().Append(OrderTime()).Append(Note()).Append(Weight()),
            [.. note, "Weight", "WeightMissing"]);
    }

    public static void Inspect()
    {
        IDataView data = Ml.Data.FilterRowsByMissingValues(
            Load(),
            "DeliveryDays");

        // How wide is each feature group?
        ITransformer full = Full(Sdca()).Fit(data);
        IDataView t = full.Transform(data);
        foreach (string c in new[] { "CityKey", "DistrictKey", "LocalDayKey", "NoteFeatures", "Features" })
        {
            Console.WriteLine($"{c,-14} {t.Schema[c].Type}");
        }

        // A few text features and their values for one note
        var note = Note().Fit(data);
        IDataView one = note.Transform(Ml.Data.LoadFromEnumerable([new DeliveryInput { CustomerNote = "office address - weekdays only" }]));
        VBuffer<ReadOnlyMemory<char>> slots = default;
        one.Schema["NoteFeatures"].GetSlotNames(ref slots);
        float[] values = one.GetColumn<float[]>("NoteFeatures").First();
        string[] names = [.. slots.DenseValues().Select(s => s.ToString())];
        Console.WriteLine("non-zero text features for 'office address - weekdays only':");
        foreach ((string n, float v) in names.Zip(values).Where(x => x.Second > 0 && x.First.StartsWith("Word.")))
        {
            Console.WriteLine($"  {n,-22} {v:F3}");
        }

        Console.WriteLine("stop word languages: " +
            string.Join(", ", Enum.GetNames<StopWordsRemovingEstimator.Language>()));

        // Missing weight without ReplaceMissingValues: what happens to the rows and to a prediction?
        var raw = Ml.Transforms.Concatenate("Features", "DistanceKm", "ItemCount", "TotalWeightKg")
            .Append(Ml.Regression.Trainers.Sdca(new SdcaRegressionTrainer.Options
            {
                LabelColumnName = "DeliveryDays", NumberOfThreads = 1,
            }));
        ITransformer rawModel = raw.Fit(data);
        var rawEngine = Ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(rawModel);
        Console.WriteLine($"weight 1.2 kg -> {rawEngine.Predict(new DeliveryInput { DistanceKm = 450, ItemCount = 2, TotalWeightKg = 1.2f }).PredictedDays:F2} days, " +
                          $"weight missing -> {rawEngine.Predict(new DeliveryInput { DistanceKm = 450, ItemCount = 2, TotalWeightKg = float.NaN }).PredictedDays}");
        float[] scores = rawModel.Transform(data).GetColumn<float>("Score").ToArray();
        Console.WriteLine($"rows scored NaN in the training data: {scores.Count(float.IsNaN)} of {scores.Length}");
    }

    public static void Skew()
    {
        IDataView data = Ml.Data.FilterRowsByMissingValues(
            Load(),
            "DeliveryDays");
        ITransformer model = Full(Tree()).Fit(data);
        Directory.CreateDirectory("models");
        Ml.Model.Save(model, data.Schema, "models/delivery-features.zip");

        // The zip stores the mapping's contract name and assembly name; that assembly must be deployable next to the model
        MLContext fresh = new();
        ITransformer loaded = fresh.Model.Load("models/delivery-features.zip", out _);
        var engine = fresh.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(loaded);

        // Thursday 14:10 in Istanbul = 11:10 UTC. A bug elsewhere sends local time as if it were UTC.
        DeliveryInput order = new()
        {
            OrderedAtUtc = new DateTime(2026, 10, 15, 11, 10, 0, DateTimeKind.Utc),
            City = "Ankara", District = "ANK-07", Warehouse = "ANK-1", DistanceKm = 20, Carrier = "CarrierA",
            ShippingMethod = "Standard", ItemCount = 1, TotalWeightKg = 0.6f, CustomerNote = "",
        };
        float correct = engine.Predict(order).PredictedDays;
        order.OrderedAtUtc = new DateTime(2026, 10, 15, 14, 10, 0, DateTimeKind.Utc);
        float skewed = engine.Predict(order).PredictedDays;
        Console.WriteLine($"ordered 14:10 local, sent as UTC 11:10 : {correct:F2} days");
        Console.WriteLine($"same order, local time sent as UTC      : {skewed:F2} days");
    }

    public static IDataView Load() => Ml.Data.LoadFromTextFile<DeliveryInput>("data/orders_clean.csv",
        new TextLoader.Options { HasHeader = true, Separators = [','], MissingRealsAsNaNs = true });

    private static double Cv(IDataView data, IEstimator<ITransformer> pipeline) =>
        Ml.Regression.CrossValidate(data, pipeline, numberOfFolds: 5, labelColumnName: "DeliveryDays",
            samplingKeyColumnName: "CustomerId", seed: 7).Average(f => f.Metrics.MeanAbsoluteError);
}
