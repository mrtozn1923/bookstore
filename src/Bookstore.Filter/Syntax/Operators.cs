namespace Bookstore.Filter.Syntax;

public static class Operators
{
    public static ComparisonOperator? Comparison(TokenKind kind) => kind switch
    {
        TokenKind.Equal => ComparisonOperator.Equal,
        TokenKind.NotEqual => ComparisonOperator.NotEqual,
        TokenKind.Less => ComparisonOperator.Less,
        TokenKind.LessOrEqual => ComparisonOperator.LessOrEqual,
        TokenKind.Greater => ComparisonOperator.Greater,
        TokenKind.GreaterOrEqual => ComparisonOperator.GreaterOrEqual,
        TokenKind.Contains => ComparisonOperator.Contains,
        _ => null,
    };

    public static string Symbol(ComparisonOperator op) => op switch
    {
        ComparisonOperator.Equal => "=",
        ComparisonOperator.NotEqual => "!=",
        ComparisonOperator.Less => "<",
        ComparisonOperator.LessOrEqual => "<=",
        ComparisonOperator.Greater => ">",
        ComparisonOperator.GreaterOrEqual => ">=",
        _ => "contains",
    };
}
