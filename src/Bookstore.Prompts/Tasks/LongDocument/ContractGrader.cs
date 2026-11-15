using System.Globalization;
using System.Text.Json;
using Bookstore.Prompts.Evaluation;

namespace Bookstore.Prompts.Tasks.LongDocument;

public sealed record ContractQuestion(List<string> Keywords, List<string> Clauses, bool NotFound = false);

public static class ContractGrader
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    static readonly CompareInfo Tr = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    public static Verdict Grade(ContractDocument contract, ContractQuestion expected, string output, bool grounded)
    {
        GroundedAnswer? answer;
        try
        {
            answer = grounded
                ? JsonSerializer.Deserialize<GroundedAnswer>(output, Json)
                : JsonSerializer.Deserialize<PlainAnswer>(output, Json) is { } plain ? new GroundedAnswer([], plain.Answer) : null;
        }
        catch (JsonException)
        {
            return Verdict.Fail("not JSON");
        }
        if (answer is null)
        {
            return Verdict.Fail("empty output");
        }

        List<string> problems = [];
        if (expected.NotFound)
        {
            if (!answer.Answer.Contains(ContractPrompts.NotFound, StringComparison.Ordinal))
            {
                problems.Add($"expected {ContractPrompts.NotFound}, got \"{Shorten(answer.Answer)}\"");
            }
        }
        else
        {
            // A keyword is "10 iş günü|on iş günü": any alternative counts. Turkish casing rules
            // make "TEDARİKÇİ" match "tedarikçi".
            List<string> missing = [.. expected.Keywords.Where(k =>
                !k.Split('|').Any(alternative => Tr.IndexOf(answer.Answer, alternative, CompareOptions.IgnoreCase) >= 0))];
            if (missing.Count > 0)
            {
                problems.Add($"answer \"{Shorten(answer.Answer)}\" lacks {string.Join(", ", missing)}");
            }
        }
        if (grounded)
        {
            int invented = answer.Quotes.Count(q => !contract.Contains(q.Text));
            if (invented > 0)
            {
                problems.Add($"{invented}/{answer.Quotes.Count} quotes not in the contract");
            }
            if (!expected.NotFound && !answer.Quotes.Any(q => expected.Clauses.Any(c => q.Clause.Contains(c, StringComparison.OrdinalIgnoreCase))))
            {
                problems.Add($"no quote from {string.Join(" or ", expected.Clauses)} (cited: {string.Join(", ", answer.Quotes.Select(q => q.Clause))})");
            }
        }
        return problems.Count == 0 ? Verdict.Pass(Shorten(answer.Answer)) : Verdict.Fail(string.Join("; ", problems));
    }

    static string Shorten(string text)
    {
        string oneLine = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return oneLine.Length <= 60 ? oneLine : oneLine[..57] + "...";
    }
}
