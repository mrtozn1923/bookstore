using System.Text.Encodings.Web;
using System.Text.Json;
using Bookstore.Prompts.Catalog;
using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.PolicyQa;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace Bookstore.Prompts.Lab;

public sealed record PolicyAnswer(string Id, string Version, string Model, string Answer, int ElapsedMs);

public sealed record PolicyLabel(string Id, string Version, string Model, string Label);

public static class EvalLab
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    static readonly JsonSerializerOptions Readable = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static string DataPath(string file) => Path.Combine(AppContext.BaseDirectory, "data", file);

    static string RunPath(string file) =>
        Path.Combine(Environment.GetEnvironmentVariable("BOOKSTORE_RUN_LOG") ?? ".", file);

    public static string Policy => File.ReadAllText(DataPath("return-policy-tr.md"));

    public static IReadOnlyList<TestCase<PolicyExpectation>> Cases() =>
        TestSet.LoadJsonl<PolicyExpectation>(DataPath("policy-questions.jsonl"));

    static string AnswersFile(string version, string model) => $"policy-answers-{version}-{model.Replace(':', '_')}.jsonl";

    static List<PolicyAnswer> LoadAnswers(string version, string model) =>
        [.. File.ReadLines(RunPath(AnswersFile(version, model))).Select(l => JsonSerializer.Deserialize<PolicyAnswer>(l, Json)!)];

    // Step 1: generate answers once and keep them; every grader below reads the same saved answers
    public static async Task AnswerAsync(IChatClient client, ChatOptions options, string model, string version)
    {
        PromptTemplate template = PromptCatalog.Default.Get("policy-qa", version);
        List<PolicyAnswer> answers = [];
        foreach (TestCase<PolicyExpectation> testCase in Cases())
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            ChatResponse response = await client.GetResponseAsync(
                template.Render(new Dictionary<string, string> { ["policy"] = Policy, ["question"] = testCase.Input }), options);
            int elapsed = (int)System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            answers.Add(new(testCase.Id, version, model, response.Text.Trim(), elapsed));
            Console.WriteLine($"{testCase.Id} ({elapsed} ms) {response.Text.Trim().ReplaceLineEndings(" ")}");
        }
        File.WriteAllLines(RunPath(AnswersFile(version, model)), answers.Select(a => JsonSerializer.Serialize(a, Readable)));
    }

    // Step 2: deterministic checks
    public static void Check(string version, string model)
    {
        Dictionary<string, TestCase<PolicyExpectation>> cases = Cases().ToDictionary(c => c.Id);
        int passed = 0;
        List<PolicyAnswer> answers = LoadAnswers(version, model);
        foreach (PolicyAnswer answer in answers)
        {
            TestCase<PolicyExpectation> testCase = cases[answer.Id];
            List<string> issues = PolicyChecks.Check(testCase.Input, answer.Answer, testCase.Expected, Policy);
            passed += issues.Count == 0 ? 1 : 0;
            Console.WriteLine($"{(issues.Count == 0 ? "PASS" : "FAIL")} {answer.Id} {string.Join("; ", issues)}");
        }
        Console.WriteLine($"policy-qa {version} | {model} | checks {passed}/{answers.Count}");
    }

    // Step 3: LLM-as-judge, compared with labels a person wrote after reading every answer
    public static async Task JudgeAsync(PolicyJudge judge, string judgeModel, IEnumerable<(string Version, string Model)> runs)
    {
        Dictionary<string, TestCase<PolicyExpectation>> cases = Cases().ToDictionary(c => c.Id);
        Dictionary<(string, string, string), string> labels = File.ReadLines(DataPath("policy-labels.jsonl"))
            .Select(l => JsonSerializer.Deserialize<PolicyLabel>(l, Json)!)
            .ToDictionary(l => (l.Id, l.Version, l.Model), l => l.Label);
        int agree = 0, total = 0, falsePass = 0, falseFail = 0, noVerdict = 0;
        List<object> raw = [];
        foreach ((string version, string model) in runs)
        {
            foreach (PolicyAnswer answer in LoadAnswers(version, model))
            {
                TestCase<PolicyExpectation> testCase = cases[answer.Id];
                JudgeVerdict verdict = await judge.GradeAsync(testCase.Input, testCase.Expected.Reference, answer.Answer);
                string human = labels[(answer.Id, version, model)];
                string judged = verdict.Verdict.Trim().ToLowerInvariant();
                total++;
                agree += judged == human ? 1 : 0;
                falsePass += judged == "correct" && human == "incorrect" ? 1 : 0;
                falseFail += judged == "incorrect" && human == "correct" ? 1 : 0;
                noVerdict += judged is not ("correct" or "incorrect") ? 1 : 0;
                Console.WriteLine($"{(judged == human ? "agree   " : "DISAGREE")} {answer.Id} {version} {model}: judge {judged}, person {human}");
                raw.Add(new { answer.Id, version, model, verdict.Verdict, verdict.Reasoning, human });
            }
        }
        Console.WriteLine($"judge {judgeModel}: agrees with the person on {agree}/{total}; passes the person failed {falsePass}, fails the person passed {falseFail}, no verdict {noVerdict}");
        ReportPrinter.SaveRaw($"policy-judge-{judgeModel.Replace(':', '_')}", raw);
    }

    // Step 4: position bias. Same pair, both orders; a consistent judge picks the same answer twice
    public static async Task PairwiseAsync(PolicyJudge judge, string judgeModel, (string Version, string Model) first, (string Version, string Model) second)
    {
        Dictionary<string, TestCase<PolicyExpectation>> cases = Cases().ToDictionary(c => c.Id);
        Dictionary<string, PolicyAnswer> x = LoadAnswers(first.Version, first.Model).ToDictionary(a => a.Id);
        Dictionary<string, PolicyAnswer> y = LoadAnswers(second.Version, second.Model).ToDictionary(a => a.Id);
        int consistent = 0, firstSlot = 0, total = 0;
        List<object> raw = [];
        foreach (string id in x.Keys)
        {
            TestCase<PolicyExpectation> testCase = cases[id];
            PairwiseVerdict ab = await judge.CompareAsync(testCase.Input, testCase.Expected.Reference, x[id].Answer, y[id].Answer);
            PairwiseVerdict ba = await judge.CompareAsync(testCase.Input, testCase.Expected.Reference, y[id].Answer, x[id].Answer);
            string winnerAb = ab.Winner.Trim() switch { "A" => first.Version, "B" => second.Version, _ => "none" };
            string winnerBa = ba.Winner.Trim() switch { "A" => second.Version, "B" => first.Version, _ => "none" };
            total++;
            consistent += winnerAb == winnerBa && winnerAb != "none" ? 1 : 0;
            firstSlot += (ab.Winner.Trim() == "A" ? 1 : 0) + (ba.Winner.Trim() == "A" ? 1 : 0);
            Console.WriteLine($"{id}: {first.Version} first -> {winnerAb}, {second.Version} first -> {winnerBa}{(winnerAb == winnerBa ? "" : "  (order decided)")}");
            raw.Add(new { id, ab, ba });
        }
        Console.WriteLine($"consistent {consistent}/{total}; answer in slot A won {firstSlot} of {total * 2} comparisons");
        ReportPrinter.SaveRaw($"policy-pairwise-{judgeModel.Replace(':', '_')}", raw);
    }

    // Step 5: the same saved answers through Microsoft.Extensions.AI.Evaluation
    public static async Task MeaiAsync(IChatClient judgeClient, string version, string model)
    {
        Dictionary<string, TestCase<PolicyExpectation>> cases = Cases().ToDictionary(c => c.Id);
        IEvaluator evaluator = new CompositeEvaluator(new KeyFactsEvaluator(), new EquivalenceEvaluator());
        ChatConfiguration judge = new(judgeClient);
        List<object> raw = [];
        foreach (PolicyAnswer answer in LoadAnswers(version, model))
        {
            TestCase<PolicyExpectation> testCase = cases[answer.Id];
            List<ChatMessage> messages = [new(ChatRole.User, testCase.Input)];
            ChatResponse response = new(new ChatMessage(ChatRole.Assistant, answer.Answer));
            EvaluationResult result = await evaluator.EvaluateAsync(messages, response, judge,
                [new KeyFactsContext(testCase.Expected, Policy), new EquivalenceEvaluatorContext(testCase.Expected.Reference)]);

            BooleanMetric facts = result.Get<BooleanMetric>(KeyFactsEvaluator.MetricName);
            NumericMetric equivalence = result.Get<NumericMetric>(EquivalenceEvaluator.EquivalenceMetricName);
            string diagnostics = string.Join("; ", equivalence.Diagnostics?.Select(d => d.Message) ?? []);
            Console.WriteLine($"{answer.Id} key facts {facts.Value}, equivalence {equivalence.Value?.ToString() ?? "-"} ({equivalence.Interpretation?.Rating}){(diagnostics.Length > 0 ? " " + diagnostics : "")}");
            raw.Add(new { answer.Id, Facts = facts.Value, FactsReason = facts.Reason, Equivalence = equivalence.Value, EquivalenceReason = equivalence.Reason, Diagnostics = diagnostics });
        }
        ReportPrinter.SaveRaw($"policy-meai-{version}-{model.Replace(':', '_')}", raw);
    }

    // promptfoo reads the same prompt files and test set; the catalog stays the single source
    public static void ExportPromptfoo(string folder)
    {
        Directory.CreateDirectory(Path.Combine(folder, "prompts"));
        foreach (string version in new[] { "v1", "v2" })
        {
            PromptTemplate template = PromptCatalog.Default.Get("policy-qa", version);
            object[] chat =
            [
                new { role = "system", content = template.Fill(template.System, new Dictionary<string, string> { ["policy"] = Policy }) },
                new { role = "user", content = template.User },
            ];
            File.WriteAllText(Path.Combine(folder, "prompts", $"policy-qa-{version}.json"),
                JsonSerializer.Serialize(chat, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
        List<string> yaml = [];
        foreach (TestCase<PolicyExpectation> testCase in Cases())
        {
            yaml.Add($"- description: {testCase.Id}");
            yaml.Add("  vars:");
            yaml.Add($"    question: {JsonSerializer.Serialize(testCase.Input, Readable)}");
            yaml.Add("  assert:");
            foreach (string fact in testCase.Expected.Facts)
            {
                string alternatives = JsonSerializer.Serialize(fact.Split('|'), Readable);
                string check = $"{alternatives}.some(f => output.toLocaleLowerCase('tr-TR').includes(f.toLocaleLowerCase('tr-TR')))";
                yaml.Add("    - type: javascript");
                yaml.Add($"      value: {JsonSerializer.Serialize(check, Readable)}");
                yaml.Add("      metric: facts");
            }
        }
        File.WriteAllLines(Path.Combine(folder, "tests.yaml"), yaml);
        Console.WriteLine($"exported 2 prompts and {Cases().Count} tests to {folder}");
    }
}
