using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Evaluation;

public sealed record Verdict(bool Passed, string Detail)
{
    public static Verdict Pass(string detail = "") => new(true, detail);
    public static Verdict Fail(string detail) => new(false, detail);
}

public sealed record CaseResult(string Id, Verdict Verdict, string Output, TimeSpan Elapsed);

public sealed record EvalReport(string Task, string Version, IReadOnlyList<CaseResult> Results, string? Fingerprint = null)
{
    public int Passed => Results.Count(r => r.Verdict.Passed);
    public double PassRate => Results.Count == 0 ? 0 : (double)Passed / Results.Count;
}

public sealed class EvalRunner(IChatClient client, ChatOptions baseOptions)
{
    public async Task<EvalReport> RunAsync<T>(
        PromptVersion prompt,
        IReadOnlyList<TestCase<T>> cases,
        Func<TestCase<T>, string, Verdict> grade,
        CancellationToken cancellationToken = default)
    {
        ChatOptions options = baseOptions.Clone();
        options.ResponseFormat = prompt.Format;

        List<CaseResult> results = [];
        foreach (TestCase<T> testCase in cases)
        {
            long start = Stopwatch.GetTimestamp();
            ChatResponse response = await client.GetResponseAsync(
                prompt.Render(testCase.Input), options, cancellationToken);
            Verdict verdict = grade(testCase, response.Text);
            results.Add(new CaseResult(testCase.Id, verdict, response.Text, Stopwatch.GetElapsedTime(start)));
        }
        return new EvalReport(prompt.Task, prompt.Version, results, prompt.Fingerprint);
    }
}
