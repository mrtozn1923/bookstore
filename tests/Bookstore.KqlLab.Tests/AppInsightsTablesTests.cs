using System.Text.Json.Nodes;
using KustoLoco.Core;

namespace Bookstore.KqlLab.Tests;

public sealed class AppInsightsTablesTests : IDisposable
{
    private readonly string dataDir = Directory.CreateTempSubdirectory("kqllab-").FullName;

    public void Dispose() => Directory.Delete(dataDir, recursive: true);

    [Fact]
    public async Task Sampled_request_counts_as_100_over_sample_rate()
    {
        Write(Request("a1", success: false, sampleRate: 25), Request("a2", success: true, sampleRate: 25));
        KustoQueryContext context = AppInsightsTables.Load(dataDir);

        KustoQueryResult result = await context.RunQuery(
            "requests | summarize rows = count(), estimated = sum(itemCount), failed = countif(success == \"False\")");

        Assert.Equal("", result.Error ?? "");
        object?[] row = result.GetRow(0);
        Assert.Equal(2L, Convert.ToInt64(row[0]));
        Assert.Equal(8L, Convert.ToInt64(row[1]));
        Assert.Equal(1L, Convert.ToInt64(row[2]));
    }

    [Fact]
    public async Task Workspace_table_has_a_bool_Success_and_renamed_columns()
    {
        Write(Request("a1", success: false, sampleRate: null));
        KustoQueryContext context = AppInsightsTables.Load(dataDir);

        KustoQueryResult result = await context.RunQuery(
            "AppRequests | where Success == false | project Name, DurationMs, OperationId, ItemCount");

        Assert.Equal("", result.Error ?? "");
        Assert.Equal(["POST /api/orders/", 1500.0, "op-a1", 1], result.GetRow(0));
    }

    [Fact]
    public async Task Standard_metrics_and_performance_counters_stay_out_of_customMetrics()
    {
        Write(Metric("bookstore.orders.placed", []), Metric("requests/duration", new JsonObject { ["_MS.MetricId"] = "requests/duration" }),
            Metric(@"\Process(??APP_WIN32_PROC??)\Private Bytes", []));
        KustoQueryContext context = AppInsightsTables.Load(dataDir);

        KustoQueryResult result = await context.RunQuery("customMetrics | project name");

        Assert.Equal(["bookstore.orders.placed"], result.EnumerateRows().Select(r => (string)r[0]!).ToArray());
    }

    private void Write(params string[] lines) => File.WriteAllLines(Path.Combine(dataDir, "envelopes.jsonl"), lines);

    private static string Request(string id, bool success, int? sampleRate)
    {
        JsonObject item = new()
        {
            ["name"] = "Request",
            ["time"] = "2026-11-14T21:15:34.3490000Z",
            ["tags"] = new JsonObject { ["ai.operation.id"] = $"op-{id}", ["ai.cloud.role"] = "bookstore-api" },
            ["data"] = new JsonObject
            {
                ["baseType"] = "RequestData",
                ["baseData"] = new JsonObject
                {
                    ["id"] = id,
                    ["name"] = "POST /api/orders/",
                    ["duration"] = "00:00:01.5000000",
                    ["success"] = success,
                    ["responseCode"] = success ? "201" : "502",
                    ["properties"] = new JsonObject()
                }
            }
        };
        if (sampleRate is int rate)
        {
            item["sampleRate"] = rate;
        }
        return item.ToJsonString();
    }

    private static string Metric(string name, JsonObject properties) => new JsonObject
    {
        ["name"] = "Metric",
        ["time"] = "2026-11-14T21:16:25.0000000Z",
        ["tags"] = new JsonObject(),
        ["data"] = new JsonObject
        {
            ["baseType"] = "MetricData",
            ["baseData"] = new JsonObject
            {
                ["metrics"] = new JsonArray(new JsonObject { ["name"] = name, ["value"] = 4 }),
                ["properties"] = properties
            }
        }
    }.ToJsonString();
}
