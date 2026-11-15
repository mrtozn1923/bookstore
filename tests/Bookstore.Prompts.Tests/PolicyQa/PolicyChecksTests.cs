using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.PolicyQa;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace Bookstore.Prompts.Tests.PolicyQa;

public class PolicyChecksTests
{
    static string Data(string file) => Path.Combine(AppContext.BaseDirectory, "data", file);

    static readonly string Policy = File.ReadAllText(Data("return-policy-tr.md"));

    static readonly PolicyExpectation Expected = new("Hayır, en fazla 20 kitap iade edilebilir.", ["20", "kurumsal satış"]);

    [Fact]
    public void Any_alternative_of_a_fact_counts()
    {
        PolicyExpectation expected = new("", ["49,90|müşteriye ait"]);

        Assert.Empty(PolicyChecks.Check("Kargo ücretli mi?", "Kargo ücreti müşteriye aittir.", expected, Policy));
    }

    [Fact]
    public void Numbers_that_are_not_in_the_policy_question_or_reference_are_reported()
    {
        List<string> issues = PolicyChecks.Check("40 kitabı iade edebilir miyiz?",
            "Hayır, en fazla 20 kitap; kurumsal satış onayıyla 25 kitap olabilir.", Expected, Policy);

        Assert.Equal(["numbers not in the policy: 25"], issues);
    }

    [Fact]
    public void Long_answers_fail()
    {
        string answer = "20 kitap, kurumsal satış. " + new string('x', PolicyChecks.MaxLength);

        Assert.Contains(PolicyChecks.Check("?", answer, Expected, Policy), i => i.StartsWith("too long", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_question_has_a_reference_and_facts_and_every_answer_set_has_labels()
    {
        IReadOnlyList<TestCase<PolicyExpectation>> cases = TestSet.LoadJsonl<PolicyExpectation>(Data("policy-questions.jsonl"));
        string[] labels = File.ReadAllLines(Data("policy-labels.jsonl"));

        Assert.Equal(10, cases.Count);
        Assert.All(cases, c => Assert.True(c.Expected.Reference.Length > 0 && c.Expected.Facts.Count > 0, c.Id));
        Assert.Equal(30, labels.Length);
    }

    [Fact]
    public async Task Key_facts_evaluator_returns_a_failed_boolean_metric_with_the_reason()
    {
        KeyFactsEvaluator evaluator = new();
        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, "Hayır, 40 kitap iade edilemez."));

        EvaluationResult result = await evaluator.EvaluateAsync(
            [new(ChatRole.User, "40 kitabı iade edebilir miyiz?")], response, null, [new KeyFactsContext(Expected, Policy)],
            TestContext.Current.CancellationToken);

        BooleanMetric metric = result.Get<BooleanMetric>(KeyFactsEvaluator.MetricName);
        Assert.False(metric.Value);
        Assert.Equal("missing fact: 20; missing fact: kurumsal satış", metric.Reason);
        Assert.True(metric.Interpretation!.Failed);
    }
}
