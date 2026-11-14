using System.Globalization;
using System.Text.Json.Nodes;
using KustoLoco.Core;

namespace Bookstore.KqlLab;

// Classic Application Insights tables (what the Logs pane of an Application Insights resource shows) and their
// workspace-based counterparts (what a Log Analytics workspace shows). Column names and types follow the
// "Table schemas" mapping in Microsoft Learn. Columns that the ingestion service derives on its own
// (client_*, performanceBucket, appId, problemId, itemId…) are left out.
public sealed record RequestRow(DateTime timestamp, string id, string name, string url, string success, string resultCode,
    double duration, string itemType, JsonNode customDimensions, string operation_Name, string operation_Id,
    string operation_ParentId, string cloud_RoleName, string cloud_RoleInstance, string application_Version,
    string sdkVersion, int itemCount);

public sealed record DependencyRow(DateTime timestamp, string id, string target, string type, string name, string data,
    string success, string resultCode, double duration, string itemType, JsonNode customDimensions, string operation_Name,
    string operation_Id, string operation_ParentId, string cloud_RoleName, string cloud_RoleInstance,
    string application_Version, string sdkVersion, int itemCount);

public sealed record TraceRow(DateTime timestamp, string message, int? severityLevel, string itemType,
    JsonNode customDimensions, string operation_Name, string operation_Id, string operation_ParentId,
    string cloud_RoleName, string cloud_RoleInstance, string application_Version, string sdkVersion, int itemCount);

public sealed record ExceptionRow(DateTime timestamp, string type, string message, string outerType, string outerMessage,
    string innermostType, string innermostMessage, int? severityLevel, JsonNode details, string itemType,
    JsonNode customDimensions, string operation_Name, string operation_Id, string operation_ParentId,
    string cloud_RoleName, string cloud_RoleInstance, string application_Version, string sdkVersion, int itemCount);

public sealed record MetricRow(DateTime timestamp, string name, double value, int valueCount, double valueSum,
    double valueMin, double valueMax, double valueStdDev, string itemType, JsonNode customDimensions,
    string operation_Name, string operation_Id, string operation_ParentId, string cloud_RoleName,
    string cloud_RoleInstance, string application_Version, string sdkVersion);

public sealed record AppRequestRow(DateTime TimeGenerated, string Id, string Name, string Url, bool Success,
    string ResultCode, double DurationMs, JsonNode Properties, string OperationName, string OperationId, string ParentId,
    string AppRoleName, string AppRoleInstance, string AppVersion, string SDKVersion, int ItemCount, string Type);

public sealed record AppDependencyRow(DateTime TimeGenerated, string Id, string Target, string DependencyType,
    string Name, string Data, bool Success, string ResultCode, double DurationMs, JsonNode Properties,
    string OperationName, string OperationId, string ParentId, string AppRoleName, string AppRoleInstance,
    string AppVersion, string SDKVersion, int ItemCount, string Type);

public sealed record AppTraceRow(DateTime TimeGenerated, string Message, int? SeverityLevel, JsonNode Properties,
    string OperationName, string OperationId, string ParentId, string AppRoleName, string AppRoleInstance,
    string AppVersion, string SDKVersion, int ItemCount, string Type);

public sealed record AppExceptionRow(DateTime TimeGenerated, string ExceptionType, string Message, string OuterType,
    string OuterMessage, string InnermostType, string InnermostMessage, int? SeverityLevel, JsonNode Details,
    JsonNode Properties, string OperationName, string OperationId, string ParentId, string AppRoleName,
    string AppRoleInstance, string AppVersion, string SDKVersion, int ItemCount, string Type);

public sealed record AppMetricRow(DateTime TimeGenerated, string Name, int ItemCount, double Sum, double Min, double Max,
    JsonNode Properties, string OperationName, string OperationId, string ParentId, string AppRoleName,
    string AppRoleInstance, string AppVersion, string SDKVersion, string Type);

public static class AppInsightsTables
{
    private static readonly string[] SeverityLevels = ["Verbose", "Information", "Warning", "Error", "Critical"];

    public static KustoQueryContext Load(string dataDir)
    {
        List<RequestRow> requests = [];
        List<DependencyRow> dependencies = [];
        List<TraceRow> traces = [];
        List<ExceptionRow> exceptions = [];
        List<MetricRow> customMetrics = [];

        foreach (string line in File.ReadLines(Path.Combine(dataDir, "envelopes.jsonl")))
        {
            JsonNode item = JsonNode.Parse(line)!;
            JsonNode tags = item["tags"] ?? new JsonObject();
            JsonNode data = item["data"]!["baseData"]!;
            Common c = new(
                DateTime.Parse((string)item["time"]!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                Tag(tags, "ai.operation.name"), Tag(tags, "ai.operation.id"), Tag(tags, "ai.operation.parentId"),
                Tag(tags, "ai.cloud.role"), Tag(tags, "ai.cloud.roleInstance"), Tag(tags, "ai.application.ver"),
                Tag(tags, "ai.internal.sdkVersion"),
                // A sampled item stands for 100 / sampleRate items; the ingestion service stores that as itemCount.
                item["sampleRate"] is JsonNode rate ? (int)Math.Round(100 / (double)rate) : 1,
                data["properties"]?.DeepClone() ?? new JsonObject());

            switch ((string)item["name"]!)
            {
                case "Request":
                    requests.Add(new RequestRow(c.Time, Str(data, "id"), Str(data, "name"), Str(data, "url"),
                        Bool(data), Str(data, "responseCode"), Ms(data), "request", c.Properties, c.OperationName,
                        c.OperationId, c.ParentId, c.Role, c.RoleInstance, c.Version, c.Sdk, c.ItemCount));
                    break;
                case "RemoteDependency":
                    dependencies.Add(new DependencyRow(c.Time, Str(data, "id"), Str(data, "target"), Str(data, "type"),
                        Str(data, "name"), Str(data, "data"), Bool(data), Str(data, "resultCode"), Ms(data), "dependency",
                        c.Properties, c.OperationName, c.OperationId, c.ParentId, c.Role, c.RoleInstance, c.Version,
                        c.Sdk, c.ItemCount));
                    break;
                case "Message":
                    traces.Add(new TraceRow(c.Time, Str(data, "message"), Severity(data), "trace", c.Properties,
                        c.OperationName, c.OperationId, c.ParentId, c.Role, c.RoleInstance, c.Version, c.Sdk,
                        c.ItemCount));
                    break;
                case "Exception":
                    JsonArray chain = data["exceptions"]!.AsArray();
                    JsonNode outer = chain[0]!;
                    JsonNode innermost = chain[^1]!;
                    exceptions.Add(new ExceptionRow(c.Time, Str(outer, "typeName"), Str(outer, "message"),
                        Str(outer, "typeName"), Str(outer, "message"), Str(innermost, "typeName"),
                        Str(innermost, "message"), Severity(data), chain.DeepClone(), "exception", c.Properties,
                        c.OperationName, c.OperationId, c.ParentId, c.Role, c.RoleInstance, c.Version, c.Sdk,
                        c.ItemCount));
                    break;
                case "Metric":
                    JsonNode point = data["metrics"]![0]!;
                    string name = Str(point, "name");
                    // Standard metrics (_MS.MetricId) and performance counters ("\Process(…)") are stored elsewhere;
                    // _OTELRESOURCE_ carries the resource attributes. Only custom metrics land in customMetrics.
                    if (c.Properties["_MS.MetricId"] is not null || name.StartsWith('\\') || name == "_OTELRESOURCE_")
                    {
                        break;
                    }
                    double value = (double)point["value"]!;
                    customMetrics.Add(new MetricRow(c.Time, name, value, (int?)point["count"] ?? 1, value,
                        (double?)point["min"] ?? value, (double?)point["max"] ?? value, (double?)point["stdDev"] ?? 0,
                        "customMetric", c.Properties, c.OperationName, c.OperationId, c.ParentId, c.Role,
                        c.RoleInstance, c.Version, c.Sdk));
                    break;
            }
        }

        KustoQueryContext context = new();
        context.CopyDataIntoTable("requests", requests);
        context.CopyDataIntoTable("dependencies", dependencies);
        context.CopyDataIntoTable("traces", traces);
        context.CopyDataIntoTable("exceptions", exceptions);
        context.CopyDataIntoTable("customMetrics", customMetrics);

        // The same rows under the workspace-based names: a Log Analytics workspace stores them this way.
        context.CopyDataIntoTable("AppRequests", [.. requests.Select(r => new AppRequestRow(r.timestamp, r.id, r.name,
            r.url, r.success == "True", r.resultCode, r.duration, r.customDimensions, r.operation_Name, r.operation_Id,
            r.operation_ParentId, r.cloud_RoleName, r.cloud_RoleInstance, r.application_Version, r.sdkVersion,
            r.itemCount, "AppRequests"))]);
        context.CopyDataIntoTable("AppDependencies", [.. dependencies.Select(d => new AppDependencyRow(d.timestamp,
            d.id, d.target, d.type, d.name, d.data, d.success == "True", d.resultCode, d.duration, d.customDimensions,
            d.operation_Name, d.operation_Id, d.operation_ParentId, d.cloud_RoleName, d.cloud_RoleInstance,
            d.application_Version, d.sdkVersion, d.itemCount, "AppDependencies"))]);
        context.CopyDataIntoTable("AppTraces", [.. traces.Select(t => new AppTraceRow(t.timestamp, t.message,
            t.severityLevel, t.customDimensions, t.operation_Name, t.operation_Id, t.operation_ParentId,
            t.cloud_RoleName, t.cloud_RoleInstance, t.application_Version, t.sdkVersion, t.itemCount, "AppTraces"))]);
        context.CopyDataIntoTable("AppExceptions", [.. exceptions.Select(e => new AppExceptionRow(e.timestamp, e.type,
            e.message, e.outerType, e.outerMessage, e.innermostType, e.innermostMessage, e.severityLevel, e.details,
            e.customDimensions, e.operation_Name, e.operation_Id, e.operation_ParentId, e.cloud_RoleName,
            e.cloud_RoleInstance, e.application_Version, e.sdkVersion, e.itemCount, "AppExceptions"))]);
        context.CopyDataIntoTable("AppMetrics", [.. customMetrics.Select(m => new AppMetricRow(m.timestamp, m.name,
            m.valueCount, m.valueSum, m.valueMin, m.valueMax, m.customDimensions, m.operation_Name, m.operation_Id,
            m.operation_ParentId, m.cloud_RoleName, m.cloud_RoleInstance, m.application_Version, m.sdkVersion,
            "AppMetrics"))]);
        return context;
    }

    private sealed record Common(DateTime Time, string OperationName, string OperationId, string ParentId, string Role,
        string RoleInstance, string Version, string Sdk, int ItemCount, JsonNode Properties);

    private static string Tag(JsonNode tags, string key) => (string?)tags[key] ?? "";

    private static string Str(JsonNode node, string key) => (string?)node[key] ?? "";

    // Classic tables keep success as the string "True"/"False"; AppRequests/AppDependencies have a bool.
    private static string Bool(JsonNode data) => (bool?)data["success"] == true ? "True" : "False";

    private static double Ms(JsonNode data) =>
        TimeSpan.Parse((string)data["duration"]!, CultureInfo.InvariantCulture).TotalMilliseconds;

    // Verbose = 0 … Critical = 4; exceptions recorded on a span carry no severity.
    private static int? Severity(JsonNode data) =>
        data["severityLevel"] is JsonNode level ? Array.IndexOf(SeverityLevels, (string)level!) : null;
}
