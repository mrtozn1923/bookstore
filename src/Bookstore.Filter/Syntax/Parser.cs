namespace Bookstore.Filter.Syntax;

// Pratt parser: precedence and associativity live in one table (BindingPower) instead of one method per level.
public sealed class Parser
{
    public const int MaxDepth = 64;

    private const int NotBindingPower = 30;

    private readonly List<Token> _tokens;
    private readonly List<Diagnostic> _diagnostics;
    private int _pos;
    private int _depth;
    private bool _tooDeep;

    private Parser(List<Token> tokens, List<Diagnostic> diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
    }

    private Token Current => _tokens[_pos];

    public static ParseResult Parse(string text)
    {
        (List<Token> tokens, List<Diagnostic> diagnostics) = Lexer.Tokenize(text);
        Parser parser = new(tokens, diagnostics);
        Expr root = parser.ParseExpression(0);
        while (parser.Current.Kind != TokenKind.EndOfInput)
        {
            parser.Error(parser.Current.Position, $"unexpected '{parser.Current.Text}' after the end of the filter");
            parser.Synchronize();
            if (parser.Current.Kind is TokenKind.And or TokenKind.Or or TokenKind.CloseParen)
                parser.Advance();
        }
        return new ParseResult(root, diagnostics);
    }

    // (left, right) binding power; left < right means left-associative
    private static (int Left, int Right)? BindingPower(TokenKind kind) => kind switch
    {
        TokenKind.Or => (10, 11),
        TokenKind.And => (20, 21),
        TokenKind.Equal or TokenKind.NotEqual or TokenKind.Less or TokenKind.LessOrEqual
            or TokenKind.Greater or TokenKind.GreaterOrEqual or TokenKind.Contains => (40, 41),
        _ => null,
    };

    private Expr ParseExpression(int minBindingPower)
    {
        if (++_depth > MaxDepth)
        {
            Error(Current.Position, $"filter is nested too deeply (limit {MaxDepth})");
            // skip to EndOfInput and stay quiet: the missing ')' errors would only repeat this one
            _tooDeep = true;
            _pos = _tokens.Count - 1;
            _depth--;
            return new ErrorExpr(Current.Position);
        }

        Expr left = ParsePrefix();
        while (BindingPower(Current.Kind) is (int leftPower, int rightPower) && leftPower >= minBindingPower)
        {
            Token op = Advance();
            Expr right = ParseExpression(rightPower);
            left = MakeInfix(left, op, right);
        }

        _depth--;
        return left;
    }

    private Expr ParsePrefix()
    {
        Token token = Current;
        switch (token.Kind)
        {
            case TokenKind.Identifier:
                Advance();
                return new Identifier(token.Text, token.Position);
            case TokenKind.Number or TokenKind.String or TokenKind.True or TokenKind.False:
                Advance();
                return new Literal(token.Value!, token.Position);
            case TokenKind.Not:
                Advance();
                return new NotExpr(ParseExpression(NotBindingPower), token.Position);
            case TokenKind.OpenParen:
                Advance();
                Expr inner = ParseExpression(0);
                if (Current.Kind == TokenKind.CloseParen)
                    Advance();
                else
                    Error(Current.Position, $"expected ')' to close '(' at {token.Position}");
                return inner;
            case TokenKind.Bad:
                // the lexer already reported this token
                Advance();
                return new ErrorExpr(token.Position);
            default:
                string found = token.Kind == TokenKind.EndOfInput ? "end of input" : $"'{token.Text}'";
                Error(token.Position, $"expected a field, a value or '(' but found {found}");
                Synchronize();
                return new ErrorExpr(token.Position);
        }
    }

    private Expr MakeInfix(Expr left, Token op, Expr right)
    {
        if (op.Kind == TokenKind.And)
            return new BinaryExpr(left, LogicalOperator.And, right, op.Position);
        if (op.Kind == TokenKind.Or)
            return new BinaryExpr(left, LogicalOperator.Or, right, op.Position);

        if (left is ComparisonExpr)
            Error(op.Position, "comparisons cannot be chained; combine them with 'and'");
        return new ComparisonExpr(left, Operators.Comparison(op.Kind)!.Value, right, op.Position);
    }

    // panic mode: skip tokens until one that can continue the filter
    private void Synchronize()
    {
        while (Current.Kind is not (TokenKind.EndOfInput or TokenKind.And or TokenKind.Or or TokenKind.CloseParen))
            Advance();
    }

    private Token Advance()
    {
        Token token = Current;
        if (token.Kind != TokenKind.EndOfInput)
            _pos++;
        return token;
    }

    private void Error(SourcePosition position, string message)
    {
        if (_tooDeep)
            return;
        _diagnostics.Add(new Diagnostic(position, message));
    }
}
