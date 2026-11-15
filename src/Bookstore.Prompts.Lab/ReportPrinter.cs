using System.Text.Json;
using Bookstore.Prompts.Evaluation;

namespace Bookstore.Prompts.Lab;

public static class ReportPrinter
{
    public static void Print(EvalReport report, string model)
    {
        Console.WriteLine($"{report.Task} {report.Version} | {model} | {report.Results.Count} cases");
        foreach (CaseResult result in report.Results)
        {
            string status = result.Verdict.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"{status} {result.Id,-6} {result.Verdict.Detail}");
        }
        Console.WriteLine($"{report.Passed}/{report.Results.Count} passed ({report.PassRate * 100:0}%)");
        SaveRawOutputs(report, model);
    }

    // Raw model outputs go to a file so every number in the report can be traced back
    public static void SaveRawOutputs(EvalReport report, string model)
    {
        string? folder = Environment.GetEnvironmentVariable("BOOKSTORE_RUN_LOG");
        if (folder is null)
        {
            return;
        }
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"{report.Task}-{report.Version}-{model.Replace(':', '_')}.jsonl");
        File.WriteAllLines(path, report.Results.Select(r => JsonSerializer.Serialize(new
        {
            r.Id,
            r.Verdict.Passed,
            r.Verdict.Detail,
            r.Output,
            ElapsedMs = (int)r.Elapsed.TotalMilliseconds,
        })));
    }

    // Raw outputs of labs that do not go through EvalRunner
    public static void SaveRaw(string name, IEnumerable<object> rows)
    {
        string? folder = Environment.GetEnvironmentVariable("BOOKSTORE_RUN_LOG");
        if (folder is null)
        {
            return;
        }
        Directory.CreateDirectory(folder);
        File.WriteAllLines(Path.Combine(folder, $"{name}.jsonl"), rows.Select(r => JsonSerializer.Serialize(r)));
    }
}
