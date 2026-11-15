using System.Text.Json;
using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.TicketRouting;

namespace Bookstore.Prompts.Lab;

public static class RoutingLab
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    static readonly List<string> Labels = [.. TicketLabels.All.Select(l => l.Name)];

    static IReadOnlyList<TestCase<string>> Cases() =>
        TestSet.LoadJsonl<string>(Path.Combine(AppContext.BaseDirectory, "data", "tickets.jsonl"));

    public static TicketRoute? Parse(string output)
    {
        try
        {
            return JsonSerializer.Deserialize<TicketRoute>(output, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static async Task<EvalReport> RunAsync(EvalRunner runner, PromptVersion prompt, bool print = true)
    {
        ConfusionMatrix matrix = new(Labels);
        EvalReport report = await runner.RunAsync(prompt, Cases(), (testCase, output) =>
        {
            string label = Parse(output)?.Label ?? "(not JSON)";
            label = label.Length <= 40 ? label : label[..37] + "...";
            matrix.Add(testCase.Expected, label);
            return label == testCase.Expected ? Verdict.Pass(label) : Verdict.Fail($"expected {testCase.Expected}, got {label}");
        });
        if (print)
        {
            ReportPrinter.Print(report, LabClient.Model);
            Console.WriteLine();
            Console.WriteLine(matrix.Render());
        }
        return report;
    }

    // Same prompt, label order reversed: a flip means the order, not the ticket, decided
    public static async Task CompareOrderAsync(EvalRunner runner)
    {
        EvalReport normal = await RunAsync(runner, TicketRoutingPrompts.Build(withExamples: true), print: false);
        EvalReport reversed = await RunAsync(runner, TicketRoutingPrompts.Build(withExamples: true, reverse: true), print: false);
        int flips = 0;
        foreach ((CaseResult a, CaseResult b) in normal.Results.Zip(reversed.Results))
        {
            string labelA = Parse(a.Output)?.Label ?? "?";
            string labelB = Parse(b.Output)?.Label ?? "?";
            if (labelA != labelB)
            {
                flips++;
                Console.WriteLine($"flip {a.Id}: {labelA} / {labelB}");
            }
        }
        Console.WriteLine($"normal {normal.Passed}/{normal.Results.Count}, reversed {reversed.Passed}/{reversed.Results.Count}, flips {flips}");
    }

    // Two small models route independently; only agreement is routed automatically
    public static async Task CrossCheckAsync(EvalRunner first, EvalRunner second)
    {
        IReadOnlyList<TestCase<string>> cases = Cases();
        PromptVersion prompt = TicketRoutingPrompts.Build(withExamples: true);
        EvalReport a = await RunAsync(first, prompt, print: false);
        EvalReport b = await RunAsync(second, prompt, print: false);

        int auto = 0, autoWrong = 0, toPerson = 0, wrongCaught = 0;
        foreach ((CaseResult x, CaseResult y) in a.Results.Zip(b.Results))
        {
            string expected = cases.Single(c => c.Id == x.Id).Expected;
            string labelX = Parse(x.Output)?.Label ?? "?";
            string labelY = Parse(y.Output)?.Label ?? "?";
            if (labelX == labelY)
            {
                auto++;
                autoWrong += labelX == expected ? 0 : 1;
                continue;
            }
            toPerson++;
            wrongCaught += labelX == expected ? 0 : 1;
            Console.WriteLine($"disagree {x.Id}: {labelX} / {labelY} (expected {expected})");
        }
        Console.WriteLine($"first model {a.Passed}/{cases.Count}, second model {b.Passed}/{cases.Count}");
        Console.WriteLine($"agreed {auto} (wrong {autoWrong}), sent to a person {toPerson} (first model was wrong in {wrongCaught})");
    }

    // Does the model's own confidence separate right from wrong answers?
    public static async Task ConfidenceAsync(EvalRunner runner)
    {
        EvalReport report = await RunAsync(runner, TicketRoutingPrompts.Build(withExamples: true, askConfidence: true), print: false);
        List<(bool Passed, double Confidence)> points =
            [.. report.Results.Select(r => (r.Verdict.Passed, Parse(r.Output)?.Confidence ?? double.NaN))];
        Console.WriteLine($"accuracy {report.Passed}/{report.Results.Count}");
        Console.WriteLine($"mean confidence when right: {points.Where(p => p.Passed).Average(p => p.Confidence):0.00}");
        List<double> wrong = [.. points.Where(p => !p.Passed).Select(p => p.Confidence)];
        Console.WriteLine($"mean confidence when wrong: {(wrong.Count == 0 ? double.NaN : wrong.Average()):0.00}");
        Console.WriteLine($"wrong answers with confidence >= 0.9: {points.Count(p => !p.Passed && p.Confidence >= 0.9)}");
        Console.WriteLine($"distinct confidence values: {string.Join(", ", points.Select(p => p.Confidence).Distinct().Order())}");
    }
}
