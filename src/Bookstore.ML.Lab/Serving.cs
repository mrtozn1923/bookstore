using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Bookstore.ML.Lab;

/// <summary>Post 10: train a versioned model with its card, deploy it, load-test the API and watch for drift.</summary>
public static class Serving
{
    private static readonly DateOnly DataStart = new(2024, 11, 1);

    private static List<DeliveryInput> Delivered() =>
        [.. new MLContext(seed: 1).Data.CreateEnumerable<DeliveryInput>(FeatureLab.Load(), reuseRowObject: false)
            .Where(o => !float.IsNaN(o.DeliveryDays))];

    private static DateTime Utc(DateOnly d) => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>Train on [DataStart, until), check on the next 28 days, save model + card under models/.</summary>
    public static void Train(DateOnly until, string version)
    {
        MLContext ml = new(seed: 1);
        List<DeliveryInput> all = Delivered();
        List<DeliveryInput> train = [.. all.Where(o => o.OrderedAtUtc < Utc(until))];
        List<DeliveryInput> holdout = [.. all.Where(o => o.OrderedAtUtc >= Utc(until) && o.OrderedAtUtc < Utc(until.AddDays(28)))];

        IDataView trainData = ml.Data.LoadFromEnumerable(train);
        ITransformer model = FeatureLab.Full(ml.Regression.Trainers.Sdca(new SdcaRegressionTrainer.Options
        {
            LabelColumnName = "DeliveryDays", FeatureColumnName = "Features", NumberOfThreads = 1,
        })).Fit(trainData);
        RegressionMetrics m = ml.Regression.Evaluate(model.Transform(ml.Data.LoadFromEnumerable(holdout)), labelColumnName: "DeliveryDays");

        ModelCard card = new(version, "SDCA", DataStart, until, train.Count,
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes("data/orders_clean.csv")))[..16],
            Math.Round(m.MeanAbsoluteError, 3), Math.Round(m.RootMeanSquaredError, 3),
            train.Max(o => o.DistanceKm), train.Max(o => o.ItemCount),
            [.. train.Select(o => o.Warehouse).Distinct().Order()], [.. train.Select(o => o.Carrier).Distinct().Order()]);
        Directory.CreateDirectory("models");
        ml.Model.Save(model, trainData.Schema, $"models/{version}.zip");
        card.Save($"models/{version}.json");
        Console.WriteLine($"{version}: {train.Count:N0} orders before {until:yyyy-MM-dd}, holdout {holdout.Count:N0} orders, " +
                          $"MAE {card.HoldoutMae:F3}, RMSE {card.HoldoutRmse:F3}, {new FileInfo($"models/{version}.zip").Length / 1024} KB");
    }

    /// <summary>Put a version where the API reads it: models/current/delivery.zip and .json.</summary>
    public static void Deploy(string version, bool atomic)
    {
        Directory.CreateDirectory("models/current");
        foreach (string ext in new[] { "zip", "json" })
        {
            string target = $"models/current/delivery.{ext}";
            if (atomic)
            {
                // Write next to the target, then rename: readers never see a half-written file
                File.Copy($"models/{version}.{ext}", target + ".tmp", overwrite: true);
                File.Move(target + ".tmp", target, overwrite: true);
            }
            else
            {
                File.Copy($"models/{version}.{ext}", target, overwrite: true);
            }
        }
        Console.WriteLine($"deployed {version} ({(atomic ? "copy + rename" : "copy over")})");
    }

    /// <summary>Month by month after training: error, bias, unseen inputs and how far the distance mix moved (PSI).</summary>
    public static void Drift(string version)
    {
        MLContext ml = new(seed: 1);
        ModelCard card = ModelCard.Load($"models/{version}.json");
        ITransformer model = ml.Model.Load($"models/{version}.zip", out _);
        List<DeliveryInput> all = Delivered();
        float[] edges = [50, 200, 400, 600, 800, 1000, 1300];
        double[] reference = Shares(all.Where(o => o.OrderedAtUtc < Utc(card.TrainedUntil)), edges);

        Console.WriteLine($"{version}, trained before {card.TrainedUntil:yyyy-MM-dd}, holdout MAE {card.HoldoutMae:F3}");
        Console.WriteLine($"{"month",-9}{"orders",7}{"MAE",7}{"bias",7}{"CarrierB MAE",14}{"out of range",14}{"PSI distance",14}");
        for (DateOnly month = new(card.TrainedUntil.Year, card.TrainedUntil.Month, 1); month < new DateOnly(2026, 11, 1); month = month.AddMonths(1))
        {
            List<DeliveryInput> rows = [.. all.Where(o => o.OrderedAtUtc >= Utc(month) && o.OrderedAtUtc < Utc(month.AddMonths(1)))];
            float[] predicted = [.. model.Transform(ml.Data.LoadFromEnumerable(rows)).GetColumn<float>("Score")];
            double mae = rows.Select((o, i) => Math.Abs(predicted[i] - o.DeliveryDays)).Average();
            double bias = rows.Select((o, i) => predicted[i] - o.DeliveryDays).Average();
            double maeB = rows.Select((o, i) => (o, i)).Where(x => x.o.Carrier == "CarrierB")
                .Average(x => Math.Abs(predicted[x.i] - x.o.DeliveryDays));
            int unseen = rows.Count(o => card.OutOfRange(o) is not null);
            double[] now = Shares(rows, edges);
            double psi = now.Zip(reference).Sum(p => (p.First - p.Second) * Math.Log(p.First / p.Second));
            Console.WriteLine($"{month:yyyy-MM}  {rows.Count,7:N0}{mae,7:F3}{bias,7:F2}{maeB,14:F3}{unseen,14:N0}{psi,14:F3}");
        }
    }

    /// <summary>Share of orders per distance bin; a small floor keeps empty bins from dividing by zero.</summary>
    private static double[] Shares(IEnumerable<DeliveryInput> rows, float[] edges)
    {
        int[] counts = new int[edges.Length + 1];
        int n = 0;
        foreach (DeliveryInput o in rows)
        {
            int bin = 0;
            while (bin < edges.Length && o.DistanceKm >= edges[bin]) bin++;
            counts[bin]++;
            n++;
        }
        return [.. counts.Select(c => Math.Max(c, 0.5) / n)];
    }

    /// <summary>
    /// Send real orders in parallel and compare every answer with a single-threaded local prediction
    /// of the model version the API says it used ("delivery-v1,delivery-v2" while a new version is deployed).
    /// </summary>
    public static async Task LoadTestAsync(string url, string path, string versionList, int requests, int parallel)
    {
        MLContext ml = new(seed: 1);
        DeliveryInput[] orders = [.. Delivered().Where(o => o.OrderedAtUtc >= new DateTime(2026, 9, 1))];
        Dictionary<string, float[]> expectedByVersion = [];
        foreach (string version in versionList.Split(','))
        {
            PredictionEngine<DeliveryInput, DeliveryPrediction> local =
                ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(ml.Model.Load($"models/{version}.zip", out _));
            expectedByVersion[version] = [.. orders.Select(o => MathF.Round(local.Predict(o).PredictedDays, 2))];
        }

        using HttpClient http = new() { BaseAddress = new Uri(url) };
        ConcurrentBag<double> latencies = [];
        ConcurrentDictionary<string, int> versions = [];
        int wrong = 0, failed = 0;
        await Parallel.ForEachAsync(Enumerable.Range(0, requests), new ParallelOptions { MaxDegreeOfParallelism = parallel },
            async (n, ct) =>
            {
                int i = n % orders.Length;
                long start = Stopwatch.GetTimestamp();
                using HttpResponseMessage response = await http.PostAsJsonAsync(path, DeliveryRequest.From(orders[i]), ct);
                latencies.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                if (!response.IsSuccessStatusCode)
                {
                    Interlocked.Increment(ref failed);
                    return;
                }
                DeliveryResponse body = (await response.Content.ReadFromJsonAsync<DeliveryResponse>(ct))!;
                versions.AddOrUpdate(body.ModelVersion, 1, (_, c) => c + 1);
                if (body.PredictedDays != expectedByVersion[body.ModelVersion][i]) Interlocked.Increment(ref wrong);
            });

        double[] sorted = [.. latencies.Order()];
        double P(double q) => sorted[(int)Math.Ceiling(q * sorted.Length) - 1];
        Console.WriteLine($"{path}: {requests:N0} requests, {parallel} in parallel -> {wrong} wrong, {failed} failed; " +
                          $"latency p50 {P(0.50):F1} ms, p95 {P(0.95):F1} ms, p99 {P(0.99):F1} ms; " +
                          $"versions: {string.Join(", ", versions.OrderBy(v => v.Key).Select(v => $"{v.Key} x{v.Value}"))}");
    }
}
