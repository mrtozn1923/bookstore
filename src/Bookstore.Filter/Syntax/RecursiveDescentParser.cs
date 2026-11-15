namespace Bookstore.Filter.Syntax;

// Direct translation of docs/grammar.ebnf: one method per rule, precedence comes from the call chain.
// No error recovery and no depth limit: it stops at the first error (kept to compare with Parser).
public sealed class RecursiveDescentParser(List<Token> tokens)
{
    private int _pos;

    private Token Current => tokens[_pos];

    public static Expr Parse(string text)
    {
        (List<Token> tokens, List<Diagnostic> diagnostics) = Lexer.Tokenize(text);
        if (diagnostics.Count > 0)
            throw new FormatException(diagnostics[0].ToString());

        RecursiveDescentParser parser = new(tokens);
        Expr expr = parser.ParseOr();
        if (parser.Current.Kind != TokenKind.EndOfInput)
            throw parser.Error($"unexpected '{parser.Current.Text}'");
        return expr;
    }

    // or_expr = and_expr , { "or" , and_expr } ;
    private Expr ParseOr()
    {
        Expr left = ParseAnd();
        while (Current.Kind == TokenKind.Or)
        {
            Token op = Advance();
            left = new BinaryExpr(left, LogicalOperator.Or, ParseAnd(), op.Position);
        }
        return left;
    }

    // and_expr = not_expr , { "and" , not_expr } ;
    private Expr ParseAnd()
    {
        Expr left = ParseNot();
        while (Current.Kind == TokenKind.And)
        {
            Token op = Advance();
            left = new BinaryExpr(left, LogicalOperator.And, ParseNot(), op.Position);
        }
        return left;
    }

    // not_expr = "not" , not_expr | comparison ;
    private Expr ParseNot()
    {
        if (Current.Kind != TokenKind.Not)
            return ParseComparison();
        Token not = Advance();
        return new NotExpr(ParseNot(), not.Position);
    }

    // comparison = primary , [ comp_op , primary ] ;
    private Expr ParseComparison()
    {
        Expr left = ParsePrimary();
        if (Operators.Comparison(Current.Kind) is not ComparisonOperator op)
            return left;
        Token opToken = Advance();
        return new ComparisonExpr(left, op, ParsePrimary(), opToken.Position);
    }

    // primary = field | literal | "(" , expr , ")" ;
    private Expr ParsePrimary()
    {
        Token token = Advance();
        switch (token.Kind)
        {
            case TokenKind.Identifier:
                return new Identifier(token.Text, token.Position);
            case TokenKind.Number or TokenKind.String or TokenKind.True or TokenKind.False:
                return new Literal(token.Value!, token.Position);
            case TokenKind.OpenParen:
                Expr inner = ParseOr();
                if (Current.Kind != TokenKind.CloseParen)
                    throw Error("expected ')'");
                Advance();
                return inner;
            default:
                _pos--;
                throw Error($"expected a field, a value or '(' but found '{token.Text}'");
        }
    }

    private Token Advance()
    {
        Token token = Current;
        if (token.Kind != TokenKind.EndOfInput)
            _pos++;
        return token;
    }

    private FormatException Error(string message) =>
        new(new Diagnostic(Current.Position, message).ToString());
}
