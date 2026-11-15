using System.Diagnostics;
using System.Text.Json;
using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.LongDocument;
using Microsoft.Extensions.AI;
using OllamaSharp.Models.Chat;

namespace Bookstore.Prompts.Lab;

public static class LongDocLab
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    static string DataPath(string file) => Path.Combine(AppContext.BaseDirectory, "data", file);

    static ContractDocument Contract() => ContractDocument.Load(DataPath("supplier-contract-tr.md"));

    static IReadOnlyList<TestCase<ContractQuestion>> Questions() =>
        TestSet.LoadJsonl<ContractQuestion>(DataPath("contract-questions.jsonl"));

    // Ollama's context window is set per request; the default is too small for this contract
    public static ChatOptions Options(int contextTokens)
    {
        ChatOptions options = LabClient.Options();
        options.AdditionalProperties = new() { ["num_ctx"] = contextTokens };
        return options;
    }

    public static async Task AskAsync(IChatClient client, bool grounded, Placement placement, int contextTokens)
    {
        ContractDocument contract = Contract();
        PromptVersion prompt = ContractPrompts.Build(contract, grounded, placement);
        ChatOptions options = Options(contextTokens);
        options.ResponseFormat = prompt.Format;

        int passed = 0;
        List<CaseResult> results = [];
        IReadOnlyList<TestCase<ContractQuestion>> questions = Questions();
        Console.WriteLine($"{prompt.Task} {prompt.Version} | {LabClient.Model} | num_ctx {contextTokens}");
        foreach (TestCase<ContractQuestion> question in questions)
        {
            long start = Stopwatch.GetTimestamp();
            ChatResponse response = await client.GetResponseAsync(prompt.Render(question.Input), options);
            TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
            Verdict verdict = ContractGrader.Grade(contract, question.Expected, response.Text, grounded);
            passed += verdict.Passed ? 1 : 0;
            Console.WriteLine($"{(verdict.Passed ? "PASS" : "FAIL")} {question.Id} {elapsed.TotalSeconds,5:0.0}s in={response.Usage?.InputTokenCount} {verdict.Detail}");
            results.Add(new CaseResult(question.Id, verdict, response.Text, elapsed));
        }
        Console.WriteLine($"{passed}/{questions.Count} passed");
        ReportPrinter.SaveRawOutputs(new EvalReport(prompt.Task, $"{prompt.Version}-{contextTokens}", results), LabClient.Model);
    }

    // Same three questions, two orders: does the stable prefix get reused?
    public static async Task CacheAsync(IChatClient client)
    {
        ContractDocument contract = Contract();
        foreach (Placement placement in (Placement[])[Placement.DocumentFirst, Placement.QuestionFirst])
        {
            PromptVersion prompt = ContractPrompts.Build(contract, grounded: true, placement);
            ChatOptions options = Options(40960);
            options.ResponseFormat = prompt.Format;
            foreach (TestCase<ContractQuestion> question in Questions().Take(3))
            {
                long start = Stopwatch.GetTimestamp();
                ChatResponse response = await client.GetResponseAsync(prompt.Render(question.Input), options);
                TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
                ChatDoneResponseStream? done = response.RawRepresentation as ChatDoneResponseStream;
                double promptSeconds = (done?.PromptEvalDuration ?? 0) / 1e9;
                Console.WriteLine($"{placement,-14} {question.Id} total {elapsed.TotalSeconds,5:0.0}s prompt eval {promptSeconds,5:0.0}s tokens {response.Usage?.InputTokenCount}");
            }
        }
    }

    static string Shorten(string text) => text.Length <= 12 ? text : text[..9] + "...";

    // Map: collect return clauses chunk by chunk. Reduce: answer from the collected quotes only.
    public static async Task MapReduceAsync(IChatClient client, int sectionsPerChunk)
    {
        ContractDocument contract = Contract();
        ChatOptions mapOptions = Options(16384);
        mapOptions.ResponseFormat = ContractPrompts.Map.Format;
        // A small model can keep listing clauses forever; cap the output and treat a cut-off answer as a failed chunk
        mapOptions.MaxOutputTokens = 2048;

        List<ContractQuote> collected = [];
        long inputTokens = 0;
        int chunkNo = 0;
        foreach (string chunk in contract.Chunks(sectionsPerChunk))
        {
            chunkNo++;
            ChatResponse response = await client.GetResponseAsync(ContractPrompts.Map.Render(chunk), mapOptions);
            inputTokens += response.Usage?.InputTokenCount ?? 0;
            MapResult? result;
            try
            {
                result = JsonSerializer.Deserialize<MapResult>(response.Text, Json);
            }
            catch (JsonException)
            {
                Console.WriteLine($"map chunk {chunkNo}: output is not valid JSON ({response.Usage?.OutputTokenCount} output tokens), chunk skipped");
                continue;
            }
            List<ContractQuote> kept = [.. (result?.Quotes ?? []).Where(q => contract.Contains(q.Text))];
            collected.AddRange(kept);
            Console.WriteLine($"map chunk {chunkNo}: {result?.Quotes.Count ?? 0} quotes, {kept.Count} found in the contract: {string.Join(", ", kept.Select(q => Shorten(q.Clause)))}");
        }

        // The reduced document is only the verified quotes, a few hundred tokens
        string reduced = string.Join("\n\n", collected.DistinctBy(q => q.Text).Select(q => $"{q.Clause}. {q.Text}"));
        ContractDocument digest = new("return-clauses", reduced);
        PromptVersion prompt = ContractPrompts.Build(digest, grounded: true, Placement.DocumentFirst);
        ChatOptions options = Options(8192);
        options.ResponseFormat = prompt.Format;

        int passed = 0;
        List<CaseResult> results = [];
        IReadOnlyList<TestCase<ContractQuestion>> questions = Questions();
        foreach (TestCase<ContractQuestion> question in questions)
        {
            ChatResponse response = await client.GetResponseAsync(prompt.Render(question.Input), options);
            inputTokens += response.Usage?.InputTokenCount ?? 0;
            // Quotes are checked against the full contract, not only the digest
            Verdict verdict = ContractGrader.Grade(contract, question.Expected, response.Text, grounded: true);
            passed += verdict.Passed ? 1 : 0;
            Console.WriteLine($"{(verdict.Passed ? "PASS" : "FAIL")} {question.Id} {verdict.Detail}");
            results.Add(new CaseResult(question.Id, verdict, response.Text, TimeSpan.Zero));
        }
        ReportPrinter.SaveRawOutputs(new EvalReport("contract-reduce", "v1", results), LabClient.Model);
        Console.WriteLine($"{passed}/{questions.Count} passed, {collected.Count} quotes collected, {inputTokens} input tokens in total");
    }
}
