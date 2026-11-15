using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Bookstore.Prompts.Tasks.ReportSql;

public sealed record SqlCheck(IReadOnlyList<string> Errors, string? NormalizedSql)
{
    public bool Ok => Errors.Count == 0;
}

// Parses generated T-SQL with SQL Server's own parser and allows one read-only SELECT on the reporting views
public static partial class SqlGuard
{
    public static SqlCheck Check(string sql)
    {
        TSql170Parser parser = new(initialQuotedIdentifiers: true);
        TSqlFragment fragment = parser.Parse(new StringReader(sql), out IList<ParseError> parseErrors);
        if (parseErrors.Count > 0)
        {
            return new([.. parseErrors.Select(e => $"syntax error at line {e.Line}: {e.Message}")], null);
        }

        List<TSqlStatement> statements = [.. ((TSqlScript)fragment).Batches.SelectMany(b => b.Statements)];
        if (statements.Count != 1)
        {
            return new([$"expected exactly one statement, got {statements.Count}"], null);
        }
        if (statements[0] is not SelectStatement select)
        {
            return new([$"only SELECT is allowed, got {statements[0].GetType().Name}"], null);
        }

        ReferenceCollector references = new();
        select.Accept(references);
        List<string> errors = [.. references.Errors];
        if (select.Into is not null)
        {
            errors.Add("SELECT INTO is not allowed");
        }

        // alias or view name -> view; CTE and derived table names are allowed but not checked
        Dictionary<string, ReportTable> sources = new(StringComparer.OrdinalIgnoreCase);
        foreach (NamedTableReference table in references.Tables)
        {
            string? schema = table.SchemaObject.SchemaIdentifier?.Value;
            string name = table.SchemaObject.BaseIdentifier.Value;
            if (schema is null && references.LocalNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            if (schema is not null && !schema.Equals(ReportingSchema.Schema, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"schema {schema} is not allowed");
            }
            else if (ReportingSchema.Find(name) is { } view)
            {
                sources[name] = view;
                if (table.Alias?.Value is { } alias)
                {
                    sources[alias] = view;
                }
            }
            else
            {
                errors.Add($"unknown table {name}");
            }
        }

        foreach (IList<Identifier> parts in references.Columns)
        {
            string column = parts[^1].Value;
            if (parts.Count >= 2)
            {
                // a.Column or reporting.Books.Column: the qualifier decides which view must have the column
                if (sources.TryGetValue(parts[^2].Value, out ReportTable? view) && !ReportingSchema.HasColumn(view, column))
                {
                    errors.Add($"unknown column {parts[^2].Value}.{column}");
                }
            }
            else if (!sources.Values.Any(v => ReportingSchema.HasColumn(v, column))
                && !references.LocalNames.Contains(column, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"unknown column {column}");
            }
        }

        foreach (string variable in references.Variables.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!ReportingSchema.Parameters.Contains(variable, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"unknown parameter {variable}");
            }
        }
        foreach (string literal in references.Strings.Where(s => DateLiteral().IsMatch(s)))
        {
            errors.Add($"date literal '{literal}': use @StartDate and @EndDate");
        }
        if (references.BetweenEndDate)
        {
            errors.Add("@EndDate is exclusive: use >= @StartDate AND < @EndDate instead of BETWEEN");
        }

        new Sql170ScriptGenerator().GenerateScript(select, out string normalized);
        return new([.. errors.Distinct()], normalized);
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}")]
    private static partial Regex DateLiteral();

    sealed class ReferenceCollector : TSqlFragmentVisitor
    {
        public List<NamedTableReference> Tables { get; } = [];
        public List<IList<Identifier>> Columns { get; } = [];
        public List<string> LocalNames { get; } = [];
        public List<string> Variables { get; } = [];
        public List<string> Strings { get; } = [];
        public List<string> Errors { get; } = [];
        public bool BetweenEndDate { get; private set; }

        public override void Visit(TableReference node)
        {
            if (node is not (NamedTableReference or JoinTableReference or QueryDerivedTable or JoinParenthesisTableReference))
            {
                Errors.Add($"table source {node.GetType().Name} is not allowed");
            }
        }

        public override void Visit(NamedTableReference node) => Tables.Add(node);

        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.ColumnType == ColumnType.Regular && node.MultiPartIdentifier is { } identifier)
            {
                Columns.Add(identifier.Identifiers);
            }
        }

        // Column aliases, derived tables and CTEs add names the query may use
        public override void Visit(SelectScalarExpression node)
        {
            if (node.ColumnName?.Value is { } alias)
            {
                LocalNames.Add(alias);
            }
        }

        public override void Visit(QueryDerivedTable node)
        {
            if (node.Alias?.Value is { } alias)
            {
                LocalNames.Add(alias);
            }
            LocalNames.AddRange(node.Columns.Select(c => c.Value));
        }

        public override void Visit(CommonTableExpression node)
        {
            LocalNames.Add(node.ExpressionName.Value);
            LocalNames.AddRange(node.Columns.Select(c => c.Value));
        }

        public override void Visit(VariableReference node) => Variables.Add(node.Name);

        public override void Visit(StringLiteral node) => Strings.Add(node.Value);

        public override void Visit(BooleanTernaryExpression node)
        {
            if (node.ThirdExpression is VariableReference { Name: var name }
                && name.Equals("@EndDate", StringComparison.OrdinalIgnoreCase))
            {
                BetweenEndDate = true;
            }
        }
    }
}
