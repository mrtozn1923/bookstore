using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace Bookstore.Prompts.Tasks.PolicyQa;

public sealed class KeyFactsContext(PolicyExpectation expected, string policy) : EvaluationContext("Key facts", expected.Reference)
{
    public PolicyExpectation Expected { get; } = expected;
    public string Policy { get; } = policy;
}

// The deterministic checks as a Microsoft.Extensions.AI.Evaluation evaluator, so they land in the same report
public sealed class KeyFactsEvaluator : IEvaluator
{
    public const string MetricName = "Key Facts";

    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        KeyFactsContext context = additionalContext?.OfType<KeyFactsContext>().Single()
            ?? throw new ArgumentException("KeyFactsContext is required", nameof(additionalContext));
        string question = messages.Last(m => m.Role == ChatRole.User).Text;
        List<string> issues = PolicyChecks.Check(question, modelResponse.Text, context.Expected, context.Policy);

        BooleanMetric metric = new(MetricName, issues.Count == 0, issues.Count == 0 ? "all checks passed" : string.Join("; ", issues));
        metric.Interpretation = new EvaluationMetricInterpretation(
            issues.Count == 0 ? EvaluationRating.Good : EvaluationRating.Unacceptable, failed: issues.Count > 0);
        return ValueTask.FromResult(new EvaluationResult(metric));
    }
}
