using System.Text.Json;
using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.Localization;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Lab;

public sealed record IncidentExpectation(List<string> InternalTerms);

public static class LocalizationLab
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    static string DataPath(string file) => Path.Combine(AppContext.BaseDirectory, "data", file);

    public static async Task TranslateAsync(EvalRunner runner, string version)
    {
        Glossary glossary = JsonSerializer.Deserialize<Glossary>(File.ReadAllText(DataPath("glossary.json")), Json)!;
        PromptVersion prompt = version switch
        {
            "v2" => TranslationPrompts.V2(glossary),
            "v3" => TranslationPrompts.V3(glossary),
            _ => TranslationPrompts.V1,
        };
        IReadOnlyList<TestCase<string>> cases = TestSet.LoadJsonl<string>(DataPath("descriptions-tr.jsonl"));
        EvalReport report = await runner.RunAsync(prompt, cases, (testCase, output) =>
        {
            List<string> issues = TranslationChecker.Check(testCase.Input, output, glossary);
            return issues.Count == 0 ? Verdict.Pass() : Verdict.Fail(string.Join("; ", issues));
        });
        ReportPrinter.Print(report, LabClient.Model);
    }

    public static async Task CampaignAsync(IChatClient client, ChatOptions options, string version)
    {
        IReadOnlyList<TestCase<Campaign>> cases = TestSet.LoadJsonl<Campaign>(DataPath("campaigns.jsonl"));
        PromptVersion prompt = version == "v1" ? CampaignRewrite.V1 : CampaignRewrite.V2;
        // v1 and v2 get one attempt; "repair" is v2 with up to three attempts and feedback
        int maxAttempts = version == "repair" ? 3 : 1;
        ConstrainedRewriter rewriter = new(client, options);
        int passed = 0;
        Console.WriteLine($"campaign-push {version} | {LabClient.Model} | {cases.Count} cases");
        foreach (TestCase<Campaign> testCase in cases)
        {
            RewriteResult result = await rewriter.RewriteAsync(
                prompt, testCase.Input, text => CampaignRewrite.Check(testCase.Expected, text), maxAttempts);
            passed += result.Issues.Count == 0 ? 1 : 0;
            string status = result.Issues.Count == 0 ? "PASS" : "FAIL";
            Console.WriteLine($"{status} {testCase.Id} attempts={result.Attempts} length={result.Text.Length} {string.Join("; ", result.Issues)}");
            Console.WriteLine($"     {result.Text}");
        }
        Console.WriteLine($"{passed}/{cases.Count} passed");
    }

    public static async Task SummaryAsync(IChatClient client, ChatOptions options)
    {
        IReadOnlyList<TestCase<IncidentExpectation>> cases = TestSet.LoadJsonl<IncidentExpectation>(DataPath("incidents.jsonl"));
        foreach (TestCase<IncidentExpectation> testCase in cases)
        {
            ChatResponse management = await client.GetResponseAsync(AudienceSummary.Management.Render(testCase.Input), options);
            ChatResponse customer = await client.GetResponseAsync(AudienceSummary.Customer.Render(testCase.Input), options);
            Print(testCase.Id, "management", management.Text, AudienceSummary.CheckManagement(testCase.Input, management.Text));
            Print(testCase.Id, "customer", customer.Text,
                AudienceSummary.CheckCustomer(testCase.Input, customer.Text, testCase.Expected.InternalTerms));
        }

        static void Print(string id, string reader, string text, List<string> issues)
        {
            Console.WriteLine($"{(issues.Count == 0 ? "PASS" : "FAIL")} {id} {reader} {string.Join("; ", issues)}");
            Console.WriteLine(string.Join('\n', text.Trim().Split('\n').Select(l => "     " + l)));
        }
    }
}
