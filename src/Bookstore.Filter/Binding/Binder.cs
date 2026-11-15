using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Binding;

public sealed class Binder(SymbolTable symbols)
{
    private readonly List<Diagnostic> _diagnostics = [];

    public static BindResult Bind(Expr root, SymbolTable symbols)
    {
        Binder binder = new(symbols);
        BoundExpr bound = binder.BindExpr(root);
        if (bound.Type is not (FilterType.Bool or FilterType.Error))
            binder.Error(root.Position, $"a filter must be true or false, but this one is {Name(bound.Type)}");
        return new BindResult(bound, binder._diagnostics);
    }

    private BoundExpr BindExpr(Expr expr) => expr switch
    {
        Identifier id => BindIdentifier(id),
        Literal literal => BindLiteral(literal),
        ComparisonExpr comparison => BindComparison(comparison),
        BinaryExpr logical => BindLogical(logical),
        NotExpr not => BindNot(not),
        _ => new BoundError(),
    };

    private BoundExpr BindIdentifier(Identifier id)
    {
        if (symbols.Lookup(id.Name) is FieldSymbol field)
            return new BoundField(field);

        string? suggestion = symbols.Names.MinBy(name => Distance(name, id.Name.ToLowerInvariant()));
        string hint = suggestion is not null && Distance(suggestion, id.Name.ToLowerInvariant()) <= 2
            ? $"; did you mean '{suggestion}'?"
            : "";
        Error(id.Position, $"unknown field '{id.Name}'{hint}");
        return new BoundError();
    }

    private static BoundLiteral BindLiteral(Literal literal) => literal.Value switch
    {
        // 300 is an int, 300.0 and 49.90 are decimals: decimal keeps the scale it was parsed with
        decimal d when d.Scale == 0 && d is >= int.MinValue and <= int.MaxValue => new BoundLiteral((int)d, FilterType.Int),
        decimal d => new BoundLiteral(d, FilterType.Decimal),
        string s => new BoundLiteral(s, FilterType.String),
        bool b => new BoundLiteral(b, FilterType.Bool),
        _ => throw new InvalidOperationException($"unexpected literal {literal.Value}"),
    };

    private BoundExpr BindComparison(ComparisonExpr c)
    {
        BoundExpr left = BindExpr(c.Left);
        BoundExpr right = BindExpr(c.Right);
        if (left.Type == FilterType.Error || right.Type == FilterType.Error)
            return new BoundError(); // the cause is already reported; do not add a second error

        string symbol = Operators.Symbol(c.Operator);
        bool numeric = IsNumeric(left.Type) && IsNumeric(right.Type);
        bool valid = c.Operator switch
        {
            ComparisonOperator.Contains => left.Type == FilterType.String && right.Type == FilterType.String,
            ComparisonOperator.Equal or ComparisonOperator.NotEqual => numeric || left.Type == right.Type,
            // ordering strings would depend on collation, so only numbers can be ordered
            _ => numeric,
        };
        if (!valid)
        {
            Error(c.Position, $"operator '{symbol}' cannot be applied to {Name(left.Type)} and {Name(right.Type)}");
            return new BoundError();
        }

        // implicit conversion: int widens to decimal, as in C#
        if (numeric && left.Type != right.Type)
        {
            if (left.Type == FilterType.Int)
                left = ToDecimal(left);
            else
                right = ToDecimal(right);
        }
        return new BoundComparison(left, c.Operator, right);
    }

    // a literal is converted now (constant folding); a field gets a conversion node that runs per row
    private static BoundExpr ToDecimal(BoundExpr operand) => operand is BoundLiteral { Value: int value }
        ? new BoundLiteral((decimal)value, FilterType.Decimal)
        : new BoundConversion(operand, FilterType.Decimal);

    private BoundExpr BindLogical(BinaryExpr b)
    {
        BoundExpr left = BindExpr(b.Left);
        BoundExpr right = BindExpr(b.Right);
        string keyword = b.Operator == LogicalOperator.And ? "and" : "or";
        RequireBool(left, b.Left, keyword);
        RequireBool(right, b.Right, keyword);
        return left.Type == FilterType.Bool && right.Type == FilterType.Bool
            ? new BoundLogical(left, b.Operator, right)
            : new BoundError();
    }

    private BoundExpr BindNot(NotExpr n)
    {
        BoundExpr operand = BindExpr(n.Operand);
        RequireBool(operand, n.Operand, "not");
        return operand.Type == FilterType.Bool ? new BoundNot(operand) : new BoundError();
    }

    private void RequireBool(BoundExpr bound, Expr source, string keyword)
    {
        if (bound.Type is not (FilterType.Bool or FilterType.Error))
            Error(source.Position, $"'{keyword}' needs true or false, but this operand is {Name(bound.Type)}");
    }

    private static bool IsNumeric(FilterType type) => type is FilterType.Int or FilterType.Decimal;

    private static string Name(FilterType type) => type.ToString().ToLowerInvariant();

    // Levenshtein distance, only used for "did you mean" hints on short names
    private static int Distance(string a, string b)
    {
        int[] previous = [.. Enumerable.Range(0, b.Length + 1)];
        for (int i = 1; i <= a.Length; i++)
        {
            int[] current = new int[b.Length + 1];
            current[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            previous = current;
        }
        return previous[b.Length];
    }

    private void Error(SourcePosition position, string message) =>
        _diagnostics.Add(new Diagnostic(position, message));
}
