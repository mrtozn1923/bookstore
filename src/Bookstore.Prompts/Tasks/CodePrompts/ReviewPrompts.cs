using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.CodePrompts;

public sealed record ReviewFinding(string Line, string Problem, string Severity, bool Certain);

public sealed record ReviewResult(List<ReviewFinding> Findings);

public sealed record TraceRow(int Quantity, string Before, string After);

// v3: the model traces the boundary values through the old and the new code before it writes findings
public sealed record ReviewTrace(List<TraceRow> Trace, List<ReviewFinding> Findings);

public static class ReviewPrompts
{
    // v1: "review this diff"
    public static readonly PromptVersion V1 = new(
        Task: "review",
        Version: "v1",
        System: "You are a senior C# developer doing code review.",
        BuildUser: diff => $"Review this diff.\n\n{diff}");

    // v2: the specification, a checklist, evidence for every finding and an empty list when nothing is wrong
    public static PromptVersion V2 { get; } = new(
        Task: "review",
        Version: "v2",
        System: $"""
            You review one diff of the Bookstore codebase against its specification.

            ## Specification
            {TestGenPrompts.Spec}

            ## Checklist
            - Does the change alter behavior? Compare every changed condition with the specification,
              especially at the boundaries.
            - Exceptions and invalid input: are they still handled as the specification says?
            - Does the diff need a new or changed test?

            ## Rules
            - Report only problems caused by this diff. Style preferences are not findings.
            - line is one changed line copied exactly from the diff, without the leading + or -.
            - Set certain to false when you are not sure; do not present a guess as a fact.
            - If you find no problem, return an empty findings list.
            Return only a JSON object that matches the schema.
            """,
        BuildUser: diff => $"<diff>\n{diff}\n</diff>",
        Format: ChatResponseFormat.ForJsonSchema<ReviewResult>());

    public static PromptVersion V3 { get; } = V2 with
    {
        Version = "v3",
        System = $"""
            You review one diff of the Bookstore codebase against its specification.

            ## Specification
            {TestGenPrompts.Spec}

            ## Steps
            1. In trace, for each quantity 0, 1, 19, 20, 5000 and 5001, write what the old code and the new
               code return: "0", "0.10" or "throws".
            2. Every quantity where before and after differ, or where after disagrees with the specification,
               is a finding. Do not report anything else.

            ## Rules
            - line is one changed line copied exactly from the diff, without the leading + or -.
            - A finding is a problem. Never list a line only to say it is correct.
            - Set certain to false when you are not sure.
            - If the trace shows no difference, return an empty findings list.
            Return only a JSON object that matches the schema.
            """,
        Format = ChatResponseFormat.ForJsonSchema<ReviewTrace>(),
    };
}
