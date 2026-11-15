using System.Text.Json;
using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.CodePrompts;
using Bookstore.Prompts.Tasks.ReportSql;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Lab;

public static class CodeLab
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    static string DataPath(string file) => Path.Combine(AppContext.BaseDirectory, "data", file);

    static IReadOnlyList<TestCase<ReportExpectation>> Questions() =>
        TestSet.LoadJsonl<ReportExpectation>(DataPath("report-questions.jsonl"));

    public static async Task ReportSqlAsync(EvalRunner runner, string version)
    {
        PromptVersion prompt = version switch
        {
            "v1" => ReportSqlPrompts.V1,
            "v2" => ReportSqlPrompts.V2,
            _ => ReportSqlPrompts.V3,
        };
        EvalReport report = await runner.RunAsync(prompt, Questions(), ReportSqlGrader.Grade);
        ReportPrinter.Print(report, LabClient.Model);
    }

    // v3 plus one repair round: the validator's errors go back to the model
    public static async Task ReportSqlRepairAsync(IChatClient client, ChatOptions baseOptions)
    {
        ChatOptions options = baseOptions.Clone();
        options.ResponseFormat = ReportSqlPrompts.V3.Format;
        IReadOnlyList<TestCase<ReportExpectation>> cases = Questions();
        List<CaseResult> results = [];
        int repaired = 0;
        foreach (TestCase<ReportExpectation> testCase in cases)
        {
            List<ChatMessage> messages = ReportSqlPrompts.V3.Render(testCase.Input);
            ChatResponse response = await client.GetResponseAsync(messages, options);
            ReportSqlAnswer answer = ReportSqlGrader.ParseAnswer(response.Text);
            if (!string.IsNullOrWhiteSpace(answer.Sql) && SqlGuard.Check(answer.Sql) is { Ok: false } check)
            {
                messages.AddRange(response.Messages);
                messages.Add(new(ChatRole.User,
                    $"The validator rejected the query: {string.Join("; ", check.Errors)}. Return the corrected JSON."));
                response = await client.GetResponseAsync(messages, options);
                repaired++;
            }
            results.Add(new(testCase.Id, ReportSqlGrader.Grade(testCase, response.Text), response.Text, TimeSpan.Zero));
        }
        EvalReport report = new("report-sql", "v3-repair", results);
        ReportPrinter.Print(report, LabClient.Model);
        Console.WriteLine($"repair rounds: {repaired}");
    }

    public static async Task TestGenAsync(IChatClient client, ChatOptions baseOptions, string version)
    {
        PromptVersion prompt = version == "v1" ? TestGenPrompts.V1 : TestGenPrompts.V2;
        ChatOptions options = baseOptions.Clone();
        options.ResponseFormat = prompt.Format;
        ChatResponse response = await client.GetResponseAsync(prompt.Render(TestGenPrompts.Spec), options);
        GeneratedTests tests = JsonSerializer.Deserialize<GeneratedTests>(response.Text, Json)!;
        TestGenReport report = TestGenChecker.Check(tests);
        TestGenReport againstPullRequest = TestGenChecker.Check(tests, PullRequestVersions.OffByOne);

        Console.WriteLine($"test-gen {version} | {LabClient.Model}");
        if (tests.Partitions is { Count: > 0 })
        {
            Console.WriteLine($"partitions: {string.Join(" | ", tests.Partitions)}");
        }
        Console.WriteLine($"quantities: {string.Join(", ", tests.Cases.Select(c => $"{c.Quantity}->{c.Expected}"))}");
        Console.WriteLine($"cases {report.Cases}, wrong expected {report.WrongExpected.Count}, missing boundaries: [{string.Join(", ", report.MissingBoundaries)}]");
        report.WrongExpected.ForEach(w => Console.WriteLine($"  wrong {w}"));
        Console.WriteLine($"against discount-off-by-one.diff: {againstPullRequest.WrongExpected.Count - report.WrongExpected.Count} new failures");
        againstPullRequest.WrongExpected.Except(report.WrongExpected).ToList().ForEach(w => Console.WriteLine($"  fails {w}"));
        ReportPrinter.SaveRaw($"test-gen-{version}-{LabClient.Model.Replace(':', '_')}", [new { version, output = response.Text }]);
    }

    public static async Task ReviewAsync(IChatClient client, ChatOptions baseOptions, string version)
    {
        (string File, string? Planted, Func<int, decimal> After)[] diffs =
        [
            ("discount-off-by-one.diff", "quantity > MinimumQuantity", PullRequestVersions.OffByOne),
            ("discount-rename.diff", null, PullRequestVersions.Rename),
        ];
        PromptVersion prompt = version switch
        {
            "v1" => ReviewPrompts.V1,
            "v2" => ReviewPrompts.V2,
            _ => ReviewPrompts.V3,
        };
        ChatOptions options = baseOptions.Clone();
        options.ResponseFormat = prompt.Format;
        List<object> raw = [];
        Console.WriteLine($"review {version} | {LabClient.Model}");
        foreach ((string file, string? planted, Func<int, decimal> after) in diffs)
        {
            string diff = File.ReadAllText(DataPath(Path.Combine("review", file)));
            ChatResponse response = await client.GetResponseAsync(prompt.Render(diff), options);
            raw.Add(new { file, output = response.Text });
            if (prompt.Format is null)
            {
                Console.WriteLine($"--- {file}");
                Console.WriteLine(response.Text.Trim());
                continue;
            }
            ReviewResult review = JsonSerializer.Deserialize<ReviewResult>(response.Text, Json)!;
            if (version == "v3")
            {
                ReviewTrace trace = JsonSerializer.Deserialize<ReviewTrace>(response.Text, Json)!;
                List<string> traceErrors = ReviewChecker.TraceErrors(trace.Trace, InstitutionalDiscount.RateFor, after);
                Console.WriteLine($"    trace rows {trace.Trace.Count}, wrong rows {traceErrors.Count}");
                traceErrors.ForEach(e => Console.WriteLine($"    trace {e}"));
            }
            ReviewScore score = ReviewChecker.Score(review, diff, planted);
            Console.WriteLine($"--- {file}: findings {score.Findings}, grounded {score.Grounded}, planted bug found: {(planted is null ? "-" : score.FoundPlantedBug)}");
            foreach (ReviewFinding finding in review.Findings)
            {
                Console.WriteLine($"  [{finding.Severity}, certain={finding.Certain}] {finding.Line} :: {finding.Problem}");
            }
        }
        ReportPrinter.SaveRaw($"review-{version}-{LabClient.Model.Replace(':', '_')}", raw);
    }
}
