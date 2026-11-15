#:package JsonSchema.Net@9.7.0
#:property PublishAot=false
using System.Text.Json;
using Json.Schema;

JsonSchema schema = JsonSchema.FromFile(Path.GetFullPath(args[0]));

// appsettings files are JSONC: skip comments, allow trailing commas
JsonDocumentOptions jsonc = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
using JsonDocument data = JsonDocument.Parse(File.ReadAllText(args[1]), jsonc);

EvaluationResults results = schema.Evaluate(data.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
Console.WriteLine($"{args[1]}: {(results.IsValid ? "valid" : "invalid")}");
foreach (EvaluationResults detail in (results.Details ?? []).Where(d => d.Errors is { Count: > 0 }))
{
    foreach ((string keyword, string message) in detail.Errors!)
    {
        Console.WriteLine($"  {detail.InstanceLocation} [{keyword}] {message}");
    }
}
