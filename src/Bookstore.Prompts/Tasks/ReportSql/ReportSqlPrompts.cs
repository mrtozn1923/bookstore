using Bookstore.Prompts.Catalog;
using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.ReportSql;

public sealed record ReportSqlAnswer(string? Sql, string? Refusal);

// v3: the decision comes first in the schema, so the model commits to it before writing SQL
public sealed record ReportSqlDecision(string Decision, string Reason, string? Sql);

// The text lives in prompts/report-sql/*.prompt.md; the schema is filled in from ReportingSchema
public static class ReportSqlPrompts
{
    static readonly Dictionary<string, string> Schema = new() { ["schema"] = ReportingSchema.Ddl() };

    public static PromptVersion V1 { get; } = PromptCatalog.Default.ToPromptVersion("report-sql", "v1", "question");

    public static PromptVersion V2 { get; } = PromptCatalog.Default.ToPromptVersion(
        "report-sql", "v2", "question", ChatResponseFormat.ForJsonSchema<ReportSqlAnswer>(), Schema);

    public static PromptVersion V3 { get; } = PromptCatalog.Default.ToPromptVersion(
        "report-sql", "v3", "question", ChatResponseFormat.ForJsonSchema<ReportSqlDecision>(), Schema);
}
