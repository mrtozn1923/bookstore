using System.Diagnostics.CodeAnalysis;
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
        (ExpressionType kind, string name) = c.Operator switch
        {
            ComparisonOperator.Equal => (ExpressionType.Equal, "op_Equality"),
            ComparisonOperator.NotEqual => (ExpressionType.NotEqual, "op_Inequality"),
            ComparisonOperator.Less => (ExpressionType.LessThan, "op_LessThan"),
            ComparisonOperator.LessOrEqual => (ExpressionType.LessThanOrEqual, "op_LessThanOrEqual"),
            ComparisonOperator.Greater => (ExpressionType.GreaterThan, "op_GreaterThan"),
            ComparisonOperator.GreaterOrEqual => (ExpressionType.GreaterThanOrEqual, "op_GreaterThanOrEqual"),
            _ => (ExpressionType.Call, ""),
        };
        if (kind == ExpressionType.Call)
            return Expression.Call(left, StringContains, right);

        // decimal and string compare through operator methods; naming them here keeps them in a Native AOT build
        MethodInfo? method = left.Type == typeof(decimal) ? OperatorMethod(typeof(decimal), name)
            : left.Type == typeof(string) ? OperatorMethod(typeof(string), name)
            : null;
        return Expression.MakeBinary(kind, left, right, liftToNull: false, method);
    }

    private static MethodInfo OperatorMethod(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string name) =>
        type.GetMethod(name, [type, type])!;

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

        // every holder type is written out, so Native AOT knows each Captured<T> at compile time
        return literal.Value switch
        {
            int value => Captured<int>.Read(value),
            decimal value => Captured<decimal>.Read(value),
            string value => Captured<string>.Read(value),
            _ => Captured<bool>.Read((bool)literal.Value),
        };
    }

    // same shape the C# compiler produces for a captured local: a field read on a closure object
    private sealed class Captured<T>(T value)
    {
        private static readonly FieldInfo ValueField = typeof(Captured<T>).GetField("value")!;

        // the field name becomes the SQL parameter name (@value, @value1, ...)
        public readonly T value = value;

        public static MemberExpression Read(T value) => Expression.Field(Expression.Constant(new Captured<T>(value)), ValueField);
    }
}
