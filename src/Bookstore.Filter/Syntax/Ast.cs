namespace Bookstore.Filter.Syntax;

public enum ComparisonOperator { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual, Contains }

public enum LogicalOperator { And, Or }

// Position points at the token that best explains the node: the operator for binary nodes.
public abstract record Expr(SourcePosition Position);

public sealed record Identifier(string Name, SourcePosition Position) : Expr(Position);

// Value is decimal, string or bool
public sealed record Literal(object Value, SourcePosition Position) : Expr(Position);

public sealed record ComparisonExpr(Expr Left, ComparisonOperator Operator, Expr Right, SourcePosition Position)
    : Expr(Position);

public sealed record BinaryExpr(Expr Left, LogicalOperator Operator, Expr Right, SourcePosition Position)
    : Expr(Position);

public sealed record NotExpr(Expr Operand, SourcePosition Position) : Expr(Position);

// stands in for a part of the input that could not be parsed; the error is already reported
public sealed record ErrorExpr(SourcePosition Position) : Expr(Position);

public sealed record ParseResult(Expr Root, IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}
