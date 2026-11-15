using System.Globalization;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.TimeSeries;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.TimeSeries;

namespace Bookstore.ML.Lab;

/// <summary>RFM features of one customer, computed from delivered orders up to the end of the data.</summary>
public sealed class CustomerRfm
{
    public string CustomerId { get; set; } = "";
    public float RecencyDays { get; set; }
    public float Frequency { get; set; }
    public float Monetary { get; set; }
    public float CampaignShare { get; set; }
    public float AvgItems { get; set; }
    public float CancelRate { get; set; }
    public float ExpressShare { get; set; }
}

public sealed class ClusterOutput
{
    public string CustomerId { get; set; } = "";
    public uint PredictedLabel { get; set; }
    public float[] Features { get; set; } = [];
}

public sealed class DailyOrders
{
    public DateTime Day { get; set; }
    public float Orders { get; set; }

    /// <summary>The same count as double: DetectSeasonality and SR-CNN read double, the spike detectors read float.</summary>
    public double OrdersAsDouble { get; set; }
    public bool IsCampaign { get; set; }
}

public sealed class SpikeOutput
{
    [VectorType(3)] public double[] Prediction { get; set; } = [];
}

public sealed class SrCnnOutput
{
    [VectorType(7)] public double[] Prediction { get; set; } = [];
}

/// <summary>Post 08: customer segments with k-means and anomaly detection on daily orders. No labels in either.</summary>
public static class Unsupervised
{
    private static readonly DateTime DataEnd = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    public static List<CustomerRfm> LoadRfm()
    {
        List<OrderRow> orders = [.. OrderRow.Load("data/orders_clean.csv")];
        return [.. orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Any(o => !o.IsCancelled))
            .Select(g =>
            {
                List<OrderRow> kept = [.. g.Where(o => !o.IsCancelled)];
                return new CustomerRfm
                {
                    CustomerId = g.Key,
                    RecencyDays = (float)(DataEnd - kept.Max(o => o.OrderedAtUtc)).TotalDays,
                    Frequency = kept.Count,
                    Monetary = (float)kept.Sum(o => o.OrderTotal),
                    CampaignShare = kept.Count(o => o.IsCampaign) / (float)kept.Count,
                    AvgItems = (float)kept.Average(o => o.ItemCount),
                    CancelRate = g.Count(o => o.IsCancelled) / (float)g.Count(),
                    ExpressShare = kept.Count(o => o.ShippingMethod == "Express") / (float)kept.Count,
                };
            })
            .OrderBy(c => c.CustomerId)];
    }

    /// <summary>log(1 + x) for the skewed columns, then every column to mean 0, variance 1.</summary>
    public static IEstimator<ITransformer> Scaled(MLContext ml) =>
        ml.Transforms.CustomMapping<CustomerRfm, LogRfm>((input, output) =>
            {
                output.LogFrequency = MathF.Log(1 + input.Frequency);
                output.LogMonetary = MathF.Log(1 + input.Monetary);
                output.LogAvgItems = MathF.Log(1 + input.AvgItems);
            }, contractName: null)
            .Append(ml.Transforms.Concatenate("Features", "RecencyDays", "LogFrequency", "LogMonetary", "CampaignShare", "LogAvgItems"))
            .Append(ml.Transforms.NormalizeMeanVariance("Features"));

    public sealed class LogRfm
    {
        public float LogFrequency { get; set; }
        public float LogMonetary { get; set; }
        public float LogAvgItems { get; set; }
    }

    private static IEstimator<ITransformer> KMeans(MLContext ml, int k) =>
        ml.Clustering.Trainers.KMeans(new KMeansTrainer.Options
        {
            FeatureColumnName = "Features", NumberOfClusters = k, NumberOfThreads = 1,
        });

    public static void Segments()
    {
        List<CustomerRfm> customers = LoadRfm();
        Console.WriteLine($"customers with at least one delivered order: {customers.Count:N0}");

        // 1) Without scaling: Monetary (thousands of TL) drowns out everything else
        MLContext ml = new(seed: 1);
        IDataView data = ml.Data.LoadFromEnumerable(customers);
        ITransformer raw = ml.Transforms.Concatenate("Features", "RecencyDays", "Frequency", "Monetary", "CampaignShare", "AvgItems")
            .Append(KMeans(ml, 4)).Fit(data);
        Profile("k = 4, raw columns", customers, ml, raw);

        // 2) Choosing k on the scaled features: average distance (elbow), Davies-Bouldin and silhouette
        Console.WriteLine();
        Console.WriteLine($"{"k",3}{"avg distance",14}{"Davies-Bouldin",16}{"silhouette",12}");
        List<string> csv = ["k,avg_distance,davies_bouldin,silhouette"];
        for (int k = 2; k <= 8; k++)
        {
            ml = new MLContext(seed: 1);
            ITransformer model = Scaled(ml).Append(KMeans(ml, k)).Fit(data);
            IDataView scored = model.Transform(data);
            ClusteringMetrics m = ml.Clustering.Evaluate(scored, scoreColumnName: "Score", featureColumnName: "Features");
            double s = Silhouette([.. ml.Data.CreateEnumerable<ClusterOutput>(scored, reuseRowObject: false)]);
            Console.WriteLine($"{k,3}{m.AverageDistance,14:F3}{m.DaviesBouldinIndex,16:F3}{s,12:F3}");
            csv.Add(string.Create(CultureInfo.InvariantCulture, $"{k},{m.AverageDistance:F4},{m.DaviesBouldinIndex:F4},{s:F4}"));
        }
        Directory.CreateDirectory("out");
        File.WriteAllLines("out/08-k.csv", csv);

        // 3) Candidate k values, described in business terms
        foreach (int k in new[] { 3, 4, 5 })
        {
            ml = new MLContext(seed: 1);
            uint[] labels = Profile($"k = {k}, log + NormalizeMeanVariance", customers, ml, Scaled(ml).Append(KMeans(ml, k)).Fit(data));
            Console.WriteLine($"  customers with 20+ items per order: " + string.Join(", ", customers.Zip(labels)
                .Where(x => x.First.AvgItems >= 20).GroupBy(x => x.Second).OrderBy(g => g.Key).Select(g => $"{g.Count()} in cluster {g.Key}")));
        }

        // 4) Same k, different seed: are the segments stable?
        Console.WriteLine();
        foreach (int k in new[] { 3, 4, 5 })
        {
            int[] a = Assign(customers, k, seed: 1), b = Assign(customers, k, seed: 2);
            int same = 0, pairs = 0;
            Random rnd = new(5);
            for (int i = 0; i < 200_000; i++)
            {
                int x = rnd.Next(a.Length), y = rnd.Next(a.Length);
                if (x == y) continue;
                pairs++;
                if ((a[x] == a[y]) == (b[x] == b[y])) same++;
            }
            Console.WriteLine($"k = {k}, seed 1 vs seed 2: {100.0 * same / pairs:F1}% of customer pairs agree on 'same segment or not'");
        }
    }

    private static int[] Assign(List<CustomerRfm> customers, int k, int seed)
    {
        MLContext ml = new(seed: seed);
        IDataView data = ml.Data.LoadFromEnumerable(customers);
        ITransformer model = Scaled(ml).Append(KMeans(ml, k)).Fit(data);
        return [.. model.Transform(data).GetColumn<uint>("PredictedLabel").Select(l => (int)l)];
    }

    private static uint[] Profile(string title, List<CustomerRfm> customers, MLContext ml, ITransformer model)
    {
        uint[] labels = [.. model.Transform(ml.Data.LoadFromEnumerable(customers)).GetColumn<uint>("PredictedLabel")];
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine($"{"cluster",8}{"customers",10}{"recency d",10}{"orders",8}{"spend TL",10}{"campaign",9}{"items",7}{"cancel",8}{"express",8}");
        foreach (IGrouping<uint, CustomerRfm> g in customers.Zip(labels).GroupBy(x => x.Second, x => x.First).OrderBy(g => g.Key))
        {
            Console.WriteLine($"{g.Key,8}{g.Count(),10:N0}{g.Average(c => c.RecencyDays),10:F0}{g.Average(c => c.Frequency),8:F1}" +
                              $"{g.Average(c => c.Monetary),10:N0}{g.Average(c => c.CampaignShare),9:P0}{g.Average(c => c.AvgItems),7:F1}" +
                              $"{g.Average(c => c.CancelRate),8:P1}{g.Average(c => c.ExpressShare),8:P0}");
        }
        return labels;
    }

    /// <summary>Mean silhouette: (b - a) / max(a, b), a = mean distance to own cluster, b = to the nearest other cluster.</summary>
    private static double Silhouette(List<ClusterOutput> rows)
    {
        int n = rows.Count;
        double total = 0;
        for (int i = 0; i < n; i++)
        {
            Dictionary<uint, (double Sum, int Count)> byCluster = [];
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                double d = 0;
                for (int f = 0; f < rows[i].Features.Length; f++)
                {
                    double diff = rows[i].Features[f] - rows[j].Features[f];
                    d += diff * diff;
                }
                (double sum, int count) = byCluster.GetValueOrDefault(rows[j].PredictedLabel);
                byCluster[rows[j].PredictedLabel] = (sum + Math.Sqrt(d), count + 1);
            }
            uint own = rows[i].PredictedLabel;
            if (!byCluster.TryGetValue(own, out (double Sum, int Count) mine)) continue;
            double a = mine.Sum / mine.Count;
            double b = byCluster.Where(c => c.Key != own).Min(c => c.Value.Sum / c.Value.Count);
            total += (b - a) / Math.Max(a, b);
        }
        return total / n;
    }

    public static List<DailyOrders> LoadDaily()
    {
        TimeSpan turkey = TimeSpan.FromHours(3);
        return [.. OrderRow.Load("data/orders_clean.csv")
            .GroupBy(o => (o.OrderedAtUtc + turkey).Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyOrders { Day = g.Key, Orders = g.Count(), OrdersAsDouble = g.Count(), IsCampaign = g.Any(o => o.IsCampaign) })];
    }

    public static void Anomalies()
    {
        MLContext ml = new(seed: 1);
        List<DailyOrders> days = LoadDaily();
        IDataView series = ml.Data.LoadFromEnumerable(days);
        Console.WriteLine($"{days.Count} days, {days.Min(d => d.Orders)}-{days.Max(d => d.Orders)} orders per day, " +
                          $"{days.Count(d => d.IsCampaign)} campaign days");
        Console.WriteLine($"DetectSeasonality: period = {ml.AnomalyDetection.DetectSeasonality(series, "OrdersAsDouble")}, " +
                          $"with randomnessThreshold 0.5: {ml.AnomalyDetection.DetectSeasonality(series, "OrdersAsDouble", randomnessThreshold: 0.5)}");

        // 1) IidSpike assumes independent days: no weekly pattern, no trend
        double[][] iid = Run<SpikeOutput>(ml, series, ml.Transforms.DetectIidSpike(
            "Prediction", "Orders", confidence: 99.0, pvalueHistoryLength: 28)).Select(p => p.Prediction).ToArray();

        // 2) SpikeBySsa learns trend and weekly seasonality from the first 120 days
        double[][] ssa = Run<SpikeOutput>(ml, series, ml.Transforms.DetectSpikeBySsa(
            "Prediction", "Orders", confidence: 99.0, pvalueHistoryLength: 28, trainingWindowSize: 120,
            seasonalityWindowSize: 7)).Select(p => p.Prediction).ToArray();

        // 3) SR-CNN over the whole series at once, with the period it should expect
        IDataView sr = ml.AnomalyDetection.DetectEntireAnomalyBySrCnn(series, "Prediction", "OrdersAsDouble",
            new SrCnnEntireAnomalyDetectorOptions
            {
                Threshold = 0.35, Sensitivity = 90, DetectMode = SrCnnDetectMode.AnomalyAndMargin, Period = 7,
            });
        double[][] srcnn = [.. ml.Data.CreateEnumerable<SrCnnOutput>(sr, reuseRowObject: false).Select(p => p.Prediction)];

        Console.WriteLine();
        Console.WriteLine($"{"detector",-12}{"alerts",7}{"in campaign",13}{"in December",13}{"other days",12}");
        List<string> csv = ["day,orders,campaign,iid,ssa,srcnn"];
        for (int i = 0; i < days.Count; i++)
        {
            csv.Add(string.Create(CultureInfo.InvariantCulture,
                $"{days[i].Day:yyyy-MM-dd},{days[i].Orders},{(days[i].IsCampaign ? 1 : 0)},{iid[i][0]},{ssa[i][0]},{srcnn[i][0]}"));
        }
        Directory.CreateDirectory("out");
        File.WriteAllLines("out/08-daily.csv", csv);
        foreach ((string name, double[][] p) in new[] { ("IidSpike", iid), ("SpikeBySsa", ssa), ("SrCnn", srcnn) })
        {
            int[] alerts = [.. Enumerable.Range(0, days.Count).Where(i => p[i][0] != 0)];
            int campaign = alerts.Count(i => days[i].IsCampaign);
            int december = alerts.Count(i => !days[i].IsCampaign && days[i].Day.Month == 12);
            int[] other = [.. alerts.Where(i => !days[i].IsCampaign && days[i].Day.Month != 12)];
            Console.WriteLine($"{name,-12}{alerts.Length,7}{campaign,13}{december,13}{other.Length,12}");
            foreach (int i in other)
            {
                Console.WriteLine($"{"",12}{days[i].Day:yyyy-MM-dd} {days[i].Day:ddd} {days[i].Orders,4} orders");
            }
        }
    }

    private static IEnumerable<T> Run<T>(MLContext ml, IDataView series, IEstimator<ITransformer> detector) where T : class, new() =>
        ml.Data.CreateEnumerable<T>(detector.Fit(series).Transform(series), reuseRowObject: false);
}
