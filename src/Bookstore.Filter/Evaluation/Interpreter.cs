using Bookstore.Catalog;
using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Evaluation;

public sealed class FilterRuntimeException(string message) : Exception(message);

// Tree-walking interpreter: evaluates the AST directly, once per book. Values are checked at run time.
public static class Interpreter
{
    public static IEnumerable<Book> Filter(IEnumerable<Book> books, Expr filter) =>
        books.Where(book => AsBool(Evaluate(filter, book), filter));

    public static object Evaluate(Expr expr, Book book) => expr switch
    {
        Literal literal => literal.Value,
        Identifier id => ReadField(id, book),
        NotExpr not => !AsBool(Evaluate(not.Operand, book), not),
        // && and || keep the short-circuit behaviour of the filter language
        BinaryExpr { Operator: LogicalOperator.And } and =>
            AsBool(Evaluate(and.Left, book), and.Left) && AsBool(Evaluate(and.Right, book), and.Right),
        BinaryExpr or =>
            AsBool(Evaluate(or.Left, book), or.Left) || AsBool(Evaluate(or.Right, book), or.Right),
        ComparisonExpr comparison => Compare(comparison, Evaluate(comparison.Left, book), Evaluate(comparison.Right, book)),
        _ => throw new FilterRuntimeException($"cannot evaluate a filter that has syntax errors (at {expr.Position})"),
    };

    private static object ReadField(Identifier id, Book book) => id.Name.ToLowerInvariant() switch
    {
        "isbn" => book.Isbn,
        "title" => book.Title,
        "author" => book.Author,
        "price" => book.Price,
        "year" => (decimal)book.Year,
        "stock" => (decimal)book.Stock,
        _ => throw new FilterRuntimeException($"unknown field '{id.Name}' at {id.Position}"),
    };

    private static bool Compare(ComparisonExpr c, object left, object right) => (left, c.Operator, right) switch
    {
        (decimal l, ComparisonOperator.Equal, decimal r) => l == r,
        (decimal l, ComparisonOperator.NotEqual, decimal r) => l != r,
        (decimal l, ComparisonOperator.Less, decimal r) => l < r,
        (decimal l, ComparisonOperator.LessOrEqual, decimal r) => l <= r,
        (decimal l, ComparisonOperator.Greater, decimal r) => l > r,
        (decimal l, ComparisonOperator.GreaterOrEqual, decimal r) => l >= r,
        (string l, ComparisonOperator.Equal, string r) => string.Equals(l, r, StringComparison.Ordinal),
        (string l, ComparisonOperator.NotEqual, string r) => !string.Equals(l, r, StringComparison.Ordinal),
        (string l, ComparisonOperator.Contains, string r) => l.Contains(r, StringComparison.Ordinal),
        (bool l, ComparisonOperator.Equal, bool r) => l == r,
        (bool l, ComparisonOperator.NotEqual, bool r) => l != r,
        _ => throw new FilterRuntimeException(
            $"operator '{Operators.Symbol(c.Operator)}' at {c.Position} cannot compare {TypeName(left)} with {TypeName(right)}"),
    };

    private static bool AsBool(object value, Expr source) => value is bool b
        ? b
        : throw new FilterRuntimeException($"expected true or false at {source.Position} but got {TypeName(value)}");

    private static string TypeName(object value) => value switch
    {
        decimal => "number",
        string => "string",
        bool => "boolean",
        _ => value.GetType().Name,
    };
}
