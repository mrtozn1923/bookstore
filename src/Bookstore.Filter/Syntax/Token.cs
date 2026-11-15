namespace Bookstore.Filter.Syntax;

public readonly record struct SourcePosition(int Line, int Column)
{
    public override string ToString() => $"{Line}:{Column}";
}

// Text is the lexeme exactly as written; Value is the parsed literal (decimal, string, bool) or null.
public readonly record struct Token(TokenKind Kind, string Text, object? Value, SourcePosition Position);

public sealed record Diagnostic(SourcePosition Position, string Message)
{
    public override string ToString() => $"error at {Position}: {Message}";
}
