using System.Text.Json;
using System.Text.RegularExpressions;
using Bookstore.Prompts.Evaluation;

namespace Bookstore.Prompts.Tasks.ReportSql;

public sealed record ReportExpectation(bool Refuse, List<string> Tables, List<string> Uses);

public static partial class ReportSqlGrader
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // v1 answers in free text: take the first fenced block, or the whole text
    public static ReportSqlAnswer ParseAnswer(string output)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(output);
            if (document.RootElement.TryGetProperty("decision", out _))
            {
                ReportSqlDecision decision = document.RootElement.Deserialize<ReportSqlDecision>(Json)!;
                return decision.Decision == "refuse" ? new(null, decision.Reason) : new(decision.Sql, null);
            }
            ReportSqlAnswer? answer = JsonSerializer.Deserialize<ReportSqlAnswer>(output, Json);
            if (answer is not null)
            {
                return answer;
            }
        }
        catch (JsonException)
        {
        }
        Match fenced = FencedBlock().Match(output);
        return new(fenced.Success ? fenced.Groups[1].Value.Trim() : output.Trim(), null);
    }

    public static Verdict Grade(TestCase<ReportExpectation> testCase, string output)
    {
        ReportSqlAnswer answer = ParseAnswer(output);
        ReportExpectation expected = testCase.Expected;
        if (string.IsNullOrWhiteSpace(answer.Sql))
        {
            return expected.Refuse ? Verdict.Pass("refused") : Verdict.Fail($"refused: {answer.Refusal}");
        }

        SqlCheck check = SqlGuard.Check(answer.Sql);
        if (!check.Ok)
        {
            return Verdict.Fail($"blocked: {check.Errors[0]}");
        }
        if (expected.Refuse)
        {
            return Verdict.Fail("valid SELECT, but a refusal was expected");
        }

        string compact = Compact(check.NormalizedSql!);
        List<string> missing =
        [
            .. expected.Tables.Where(t => !Regex.IsMatch(check.NormalizedSql!, $@"\b{t}\b", RegexOptions.IgnoreCase)),
            .. expected.Uses.Where(u => !compact.Contains(Compact(u), StringComparison.Ordinal)),
        ];
        return missing.Count == 0 ? Verdict.Pass("ok") : Verdict.Fail($"missing: {string.Join(", ", missing)}");
    }

    static string Compact(string sql) => Whitespace().Replace(sql, "").ToUpperInvariant();

    [GeneratedRegex(@"```(?:sql|tsql)?\s*\n([\s\S]*?)```", RegexOptions.IgnoreCase)]
    private static partial Regex FencedBlock();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
