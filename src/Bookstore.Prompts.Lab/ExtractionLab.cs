using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.OrderExtraction;

namespace Bookstore.Prompts.Lab;

public static class ExtractionLab
{
    public static async Task RunAsync(EvalRunner runner, PromptVersion prompt, bool resolveInCode)
    {
        IReadOnlyList<TestCase<ExpectedOrder>> raw =
            TestSet.LoadJsonl<ExpectedOrder>(Path.Combine(AppContext.BaseDirectory, "data", "order-emails.jsonl"));
        // The sent date is part of the input the model sees
        List<TestCase<ExpectedOrder>> cases =
            [.. raw.Select(c => c with { Input = new OrderEmail(c.Expected.Sent, c.Input).ToPromptText() })];

        ExtractionScore total = new();
        EvalReport report = await runner.RunAsync(prompt, cases, (testCase, output) =>
        {
            int before = total.Problems.Count;
            OrderEmail email = new(testCase.Expected.Sent, raw.Single(r => r.Id == testCase.Id).Input);
            ExtractionScorer.Add(total, email, testCase.Expected, ExtractionScorer.Parse(output), resolveInCode);
            List<string> problems = total.Problems[before..];
            return problems.Count == 0 ? Verdict.Pass() : Verdict.Fail(string.Join("; ", problems));
        });

        ReportPrinter.Print(report, LabClient.Model);
        Console.WriteLine();
        Console.WriteLine($"items found       {total.FoundItems}/{total.ExpectedItems} (extra: {total.ExtraItems})");
        Console.WriteLine($"quantity correct  {total.QuantityCorrect}/{total.ExpectedItems} (invented: {total.QuantityInvented})");
        Console.WriteLine($"isbn correct      {total.IsbnCorrect}/{total.ExpectedItems} (invented: {total.IsbnInvented})");
        Console.WriteLine($"date by model     {total.ModelDateCorrect}/{total.Emails}");
        if (resolveInCode)
        {
            Console.WriteLine($"date by code      {total.CodeDateCorrect}/{total.Emails}");
            Console.WriteLine($"quotes in email   {total.QuotesFound}/{total.Quotes}");
            Console.WriteLine();
            foreach (CaseResult result in report.Results)
            {
                Extraction? extraction = ExtractionScorer.Parse(result.Output);
                OrderEmail email = new(cases.Single(c => c.Id == result.Id).Expected.Sent,
                    raw.Single(r => r.Id == result.Id).Input);
                List<string> issues = extraction is null ? ["not JSON"] : ExtractionValidator.Validate(extraction, email);
                Console.WriteLine($"{result.Id} {(issues.Count == 0 ? "auto" : "review: " + string.Join("; ", issues))}");
            }
        }
    }
}
