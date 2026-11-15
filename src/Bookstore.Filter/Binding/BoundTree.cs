using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Binding;

// The bound tree is the AST after semantic analysis: every node knows its type, every name its symbol.
public abstract record BoundExpr(FilterType Type);

public sealed record BoundField(FieldSymbol Field) : BoundExpr(Field.Type);

public sealed record BoundLiteral(object Value, FilterType Type) : BoundExpr(Type);

// inserted by the binder, never written by the user: int widened to decimal
public sealed record BoundConversion(BoundExpr Operand, FilterType Type) : BoundExpr(Type);

public sealed record BoundComparison(BoundExpr Left, ComparisonOperator Operator, BoundExpr Right)
    : BoundExpr(FilterType.Bool);

public sealed record BoundLogical(BoundExpr Left, LogicalOperator Operator, BoundExpr Right)
    : BoundExpr(FilterType.Bool);

public sealed record BoundNot(BoundExpr Operand) : BoundExpr(FilterType.Bool);

public sealed record BoundError() : BoundExpr(FilterType.Error);

public sealed record BindResult(BoundExpr Root, IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}
