using System.Text.Json;
using Bookstore.Prompts.Evaluation;

namespace Bookstore.Prompts.Tasks.ReturnCheck;

public sealed record ReturnCheckResult(bool IsReturnRequest, string? OrderNumber);

public static class ReturnCheckGrader
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Verdict Grade(TestCase<ReturnCheckResult> testCase, string output) =>
        Grade(output, testCase.Expected, guardMessage: null);

    // The output passes through ReturnCheckGuard first, as it does in production
    public static Verdict GradeWithGuard(TestCase<ReturnCheckResult> testCase, string output) =>
        Grade(output, testCase.Expected, guardMessage: testCase.Input);

    public static Verdict Grade(string output, ReturnCheckResult expected, string? guardMessage)
    {
        ReturnCheckResult? actual;
        try
        {
            actual = JsonSerializer.Deserialize<ReturnCheckResult>(output, Json);
        }
        catch (JsonException)
        {
            return Verdict.Fail($"not JSON: {Shorten(output)}");
        }
        if (actual is null)
        {
            return Verdict.Fail("empty output");
        }
        if (guardMessage is not null)
        {
            actual = ReturnCheckGuard.Apply(actual, guardMessage);
        }

        List<string> problems = [];
        if (actual.IsReturnRequest != expected.IsReturnRequest)
        {
            problems.Add($"isReturnRequest: expected {Lower(expected.IsReturnRequest)}, got {Lower(actual.IsReturnRequest)}");
        }
        if (Normalize(actual.OrderNumber) != Normalize(expected.OrderNumber))
        {
            problems.Add($"orderNumber: expected {expected.OrderNumber ?? "null"}, got {actual.OrderNumber ?? "null"}");
        }
        return problems.Count == 0 ? Verdict.Pass() : Verdict.Fail(string.Join("; ", problems));
    }

    static string Lower(bool value) => value ? "true" : "false";

    static string? Normalize(string? orderNumber) =>
        string.IsNullOrWhiteSpace(orderNumber) ? null : orderNumber.Trim().TrimStart('#');

    static string Shorten(string text)
    {
        string oneLine = text.ReplaceLineEndings(" ").Trim();
        return oneLine.Length <= 60 ? oneLine : oneLine[..57] + "...";
    }
}
