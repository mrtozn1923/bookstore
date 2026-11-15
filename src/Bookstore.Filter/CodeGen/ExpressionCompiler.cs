using System.Linq.Expressions;
using System.Reflection;
using Bookstore.Catalog;
using Bookstore.Filter.Binding;
using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.CodeGen;

public enum LiteralMode
{
    // values are read from a holder object; EF Core turns them into SQL parameters
    Parameterized,
    // values are embedded as constants; EF Core writes them into the SQL text
    Inline,
}

// Code generation: bound tree -> Expression<Func<Book, bool>>. The tree is only ever built from a type-checked
// bound tree, so Expression.* factory methods never see mismatched operand types.
public static class ExpressionCompiler
{
    private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    public static Expression<Func<Book, bool>> Compile(BoundExpr root, LiteralMode mode = LiteralMode.Parameterized)
    {
        ParameterExpression book = Expression.Parameter(typeof(Book), "book");
        return Expression.Lambda<Func<Book, bool>>(Emit(root, book, mode), book);
    }

    private static Expression Emit(BoundExpr node, ParameterExpression book, LiteralMode mode) => node switch
    {
        BoundField field => Expression.Property(book, field.Field.Property),
        BoundLiteral literal => EmitLiteral(literal, mode),
        BoundConversion conversion => Expression.Convert(Emit(conversion.Operand, book, mode), typeof(decimal)),
        BoundNot not => Expression.Not(Emit(not.Operand, book, mode)),
        BoundLogical { Operator: LogicalOperator.And } and =>
            Expression.AndAlso(Emit(and.Left, book, mode), Emit(and.Right, book, mode)),
        BoundLogical or => Expression.OrElse(Emit(or.Left, book, mode), Emit(or.Right, book, mode)),
        BoundComparison comparison => EmitComparison(comparison, book, mode),
        _ => throw new InvalidOperationException("cannot generate code for a filter with errors"),
    };

    private static Expression EmitComparison(BoundComparison c, ParameterExpression book, LiteralMode mode)
    {
        Expression left = Emit(c.Left, book, mode);
        Expression right = Emit(c.Right, book, mode);
        return c.Operator switch
        {
            ComparisonOperator.Equal => Expression.Equal(left, right),
            ComparisonOperator.NotEqual => Expression.NotEqual(left, right),
            ComparisonOperator.Less => Expression.LessThan(left, right),
            ComparisonOperator.LessOrEqual => Expression.LessThanOrEqual(left, right),
            ComparisonOperator.Greater => Expression.GreaterThan(left, right),
            ComparisonOperator.GreaterOrEqual => Expression.GreaterThanOrEqual(left, right),
            _ => Expression.Call(left, StringContains, right),
        };
    }

    private static Expression EmitLiteral(BoundLiteral literal, LiteralMode mode)
    {
        Type type = literal.Type switch
        {
            FilterType.Int => typeof(int),
            FilterType.Decimal => typeof(decimal),
            FilterType.String => typeof(string),
            _ => typeof(bool),
        };
        if (mode == LiteralMode.Inline)
            return Expression.Constant(literal.Value, type);

        // same shape the C# compiler produces for a captured local: a field read on a closure object
        object holder = Activator.CreateInstance(typeof(Captured<>).MakeGenericType(type), literal.Value)!;
        return Expression.Field(Expression.Constant(holder), "value");
    }

    private sealed class Captured<T>(T value)
    {
        // the field name becomes the SQL parameter name (@value, @value1, ...)
        public readonly T value = value;
    }
}
