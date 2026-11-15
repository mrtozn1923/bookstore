using System.Globalization;
using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Lab;
using Bookstore.Prompts.Tasks.LongDocument;
using Bookstore.Prompts.Tasks.OrderExtraction;
using Bookstore.Prompts.Tasks.ReturnCheck;
using Bookstore.Prompts.Tasks.TicketRouting;
using Microsoft.Extensions.AI;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

string task = args.ElementAtOrDefault(0) ?? "return-check";
string version = args.ElementAtOrDefault(1) ?? "v1";
bool firstOnly = args.Contains("--first");

EvalRunner runner = new(LabClient.Create(), LabClient.Options());

switch (task)
{
    case "return-check":
    {
        IReadOnlyList<TestCase<ReturnCheckResult>> cases =
            TestSet.LoadJsonl<ReturnCheckResult>(Path.Combine(AppContext.BaseDirectory, "data", "return-check.jsonl"));
        PromptVersion prompt = version switch
        {
            "v2" => ReturnCheckPrompts.V2,
            "v3" => ReturnCheckPrompts.V3,
            _ => ReturnCheckPrompts.V1,
        };
        Func<TestCase<ReturnCheckResult>, string, Verdict> grade =
            args.Contains("--guard") ? ReturnCheckGrader.GradeWithGuard : ReturnCheckGrader.Grade;
        EvalReport report = await runner.RunAsync(prompt, firstOnly ? [.. cases.Take(1)] : cases, grade);
        ReportPrinter.Print(report, LabClient.Model);
        break;
    }
    case "order-extraction":
    {
        PromptVersion prompt = version switch
        {
            "v2" => OrderExtractionPrompts.V2,
            "v3" => OrderExtractionPrompts.V3,
            _ => OrderExtractionPrompts.V1,
        };
        await ExtractionLab.RunAsync(runner, prompt, resolveInCode: version != "v1");
        break;
    }
    case "ticket-routing":
    {
        Task work = version switch
        {
            "v1" => RoutingLab.RunAsync(runner, TicketRoutingPrompts.V1),
            "v2" => RoutingLab.RunAsync(runner, TicketRoutingPrompts.Build(withExamples: false)),
            "v3" => RoutingLab.RunAsync(runner, TicketRoutingPrompts.Build(withExamples: true)),
            "order" => RoutingLab.CompareOrderAsync(runner),
            "confidence" => RoutingLab.ConfidenceAsync(runner),
            "cross-check" => RoutingLab.CrossCheckAsync(runner, new EvalRunner(LabClient.Create("qwen3:4b"), LabClient.Options())),
            _ => throw new ArgumentException($"Unknown version: {version}"),
        };
        await work;
        break;
    }
    case "translate":
        await LocalizationLab.TranslateAsync(runner, version);
        break;
    case "campaign":
        await LocalizationLab.CampaignAsync(LabClient.Create(), LabClient.Options(), version);
        break;
    case "summary":
        await LocalizationLab.SummaryAsync(LabClient.Create(), LabClient.Options());
        break;
    case "contract":
    {
        // contract ask v2 doc-first [context] | contract cache | contract map
        IChatClient client = LabClient.Create();
        string mode = version;
        if (mode == "cache")
        {
            await LongDocLab.CacheAsync(client);
        }
        else if (mode == "map")
        {
            await LongDocLab.MapReduceAsync(client, sectionsPerChunk: 4);
        }
        else
        {
            bool grounded = args.ElementAtOrDefault(2) != "v1";
            Placement placement = args.ElementAtOrDefault(3) == "question-first" ? Placement.QuestionFirst : Placement.DocumentFirst;
            int context = int.TryParse(args.ElementAtOrDefault(4), out int n) ? n : 40960;
            await LongDocLab.AskAsync(client, grounded, placement, context);
        }
        break;
    }
    case "report-sql":
        if (version == "repair")
        {
            await CodeLab.ReportSqlRepairAsync(LabClient.Create(), LabClient.Options());
        }
        else
        {
            await CodeLab.ReportSqlAsync(runner, version);
        }
        break;
    case "test-gen":
        await CodeLab.TestGenAsync(LabClient.Create(), LabClient.Options(), version);
        break;
    case "review":
        await CodeLab.ReviewAsync(LabClient.Create(), LabClient.Options(), version);
        break;
    default:
        Console.WriteLine($"Unknown task: {task}");
        break;
}
