using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace Bookstore.Filter.Syntax;

public ref struct Lexer
{
    private static readonly FrozenDictionary<string, TokenKind> Keywords =
        new Dictionary<string, TokenKind>
        {
            ["and"] = TokenKind.And,
            ["or"] = TokenKind.Or,
            ["not"] = TokenKind.Not,
            ["contains"] = TokenKind.Contains,
            ["true"] = TokenKind.True,
            ["false"] = TokenKind.False,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TokenKind>.AlternateLookup<ReadOnlySpan<char>> KeywordLookup =
        Keywords.GetAlternateLookup<ReadOnlySpan<char>>();

    private readonly ReadOnlySpan<char> _text;
    private readonly List<Diagnostic> _diagnostics;
    private int _pos;
    private int _line = 1;
    private int _lineStart;

    public Lexer(ReadOnlySpan<char> text, List<Diagnostic> diagnostics)
    {
        _text = text;
        _diagnostics = diagnostics;
    }

    public static (List<Token> Tokens, List<Diagnostic> Diagnostics) Tokenize(ReadOnlySpan<char> text)
    {
        List<Diagnostic> diagnostics = [];
        Lexer lexer = new(text, diagnostics);
        List<Token> tokens = [];
        Token token;
        do
        {
            token = lexer.Next();
            tokens.Add(token);
        }
        while (token.Kind != TokenKind.EndOfInput);
        return (tokens, diagnostics);
    }

    public Token Next()
    {
        SkipWhitespaceAndComments();
        int start = _pos;
        SourcePosition position = Position(start);
        if (_pos >= _text.Length)
            return new Token(TokenKind.EndOfInput, "", null, position);

        char c = _text[_pos];
        switch (c)
        {
            case '(': return Take(1, TokenKind.OpenParen, start, position);
            case ')': return Take(1, TokenKind.CloseParen, start, position);
            case '=': return Take(1, TokenKind.Equal, start, position);
            // maximal munch: the longer operator is tried first
            case '<' when Peek(1) == '=': return Take(2, TokenKind.LessOrEqual, start, position);
            case '<': return Take(1, TokenKind.Less, start, position);
            case '>' when Peek(1) == '=': return Take(2, TokenKind.GreaterOrEqual, start, position);
            case '>': return Take(1, TokenKind.Greater, start, position);
            case '!' when Peek(1) == '=': return Take(2, TokenKind.NotEqual, start, position);
            case '"': return ReadString(start, position);
        }

        if (char.IsAsciiDigit(c))
            return ReadNumber(start, position);
        Rune.DecodeFromUtf16(_text[_pos..], out Rune rune, out int consumed);
        if (Rune.IsLetter(rune) || c == '_')
            return ReadIdentifierOrKeyword(start, position);

        // unknown character: report it and skip the whole code point, so recovery always makes progress
        _pos += Math.Max(consumed, 1);
        string hint = c == '!' ? " (did you mean '!='?)" : "";
        Error(position, $"unexpected character '{rune}' (U+{rune.Value:X4}){hint}");
        return Make(TokenKind.Bad, start, position);
    }

    private Token ReadIdentifierOrKeyword(int start, SourcePosition position)
    {
        bool asciiOnly = true;
        while (_pos < _text.Length)
        {
            // decode a whole code point: a letter outside the BMP is two chars (a surrogate pair)
            Rune.DecodeFromUtf16(_text[_pos..], out Rune rune, out int consumed);
            if (!Rune.IsLetterOrDigit(rune) && rune.Value != '_')
                break;
            asciiOnly &= rune.IsAscii;
            _pos += consumed;
        }

        ReadOnlySpan<char> word = _text[start.._pos];
        if (!asciiOnly)
        {
            Error(position, $"field name '{word}' must use ASCII letters only");
            return Make(TokenKind.Bad, start, position);
        }
        if (!KeywordLookup.TryGetValue(word, out TokenKind kind))
            return Make(TokenKind.Identifier, start, position);

        object? value = kind switch
        {
            TokenKind.True => true,
            TokenKind.False => false,
            _ => null,
        };
        return new Token(kind, word.ToString(), value, position);
    }

    private Token ReadNumber(int start, SourcePosition position)
    {
        while (_pos < _text.Length && char.IsAsciiDigit(_text[_pos]))
            _pos++;

        if (Peek(0) == '.')
        {
            _pos++;
            if (!char.IsAsciiDigit(Peek(0)))
            {
                Error(Position(_pos), "expected a digit after '.'");
                return Make(TokenKind.Bad, start, position);
            }
            while (_pos < _text.Length && char.IsAsciiDigit(_text[_pos]))
                _pos++;
        }

        ReadOnlySpan<char> lexeme = _text[start.._pos];
        // the language always uses '.' as the decimal separator, whatever the current culture is
        if (!decimal.TryParse(lexeme, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value))
        {
            Error(position, $"number '{lexeme}' is out of range");
            return Make(TokenKind.Bad, start, position);
        }
        return new Token(TokenKind.Number, lexeme.ToString(), value, position);
    }

    private Token ReadString(int start, SourcePosition position)
    {
        StringBuilder value = new();
        _pos++; // opening quote
        while (true)
        {
            if (_pos >= _text.Length || _text[_pos] == '\n')
            {
                Error(position, "unterminated string literal");
                return new Token(TokenKind.String, _text[start.._pos].ToString(), value.ToString(), position);
            }

            char c = _text[_pos];
            if (c == '"')
            {
                _pos++;
                return new Token(TokenKind.String, _text[start.._pos].ToString(), value.ToString(), position);
            }

            if (c == '\\')
            {
                char next = Peek(1);
                if (next is '"' or '\\')
                {
                    value.Append(next);
                    _pos += 2;
                    continue;
                }
                if (next is '\0' or '\n')
                {
                    _pos++; // the next iteration reports the unterminated string
                    continue;
                }
                Error(Position(_pos), $"unknown escape sequence '\\{next}'");
                _pos += 2;
                continue;
            }

            value.Append(c);
            _pos++;
        }
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _text.Length)
        {
            char c = _text[_pos];
            if (c == '\n')
            {
                _pos++;
                _line++;
                _lineStart = _pos;
            }
            else if (char.IsWhiteSpace(c))
            {
                _pos++;
            }
            else if (c == '#')
            {
                // comment runs to the end of the line
                while (_pos < _text.Length && _text[_pos] != '\n')
                    _pos++;
            }
            else
            {
                return;
            }
        }
    }

    private readonly char Peek(int offset) =>
        _pos + offset < _text.Length ? _text[_pos + offset] : '\0';

    private readonly SourcePosition Position(int index) => new(_line, index - _lineStart + 1);

    private Token Take(int length, TokenKind kind, int start, SourcePosition position)
    {
        _pos += length;
        return Make(kind, start, position);
    }

    private readonly Token Make(TokenKind kind, int start, SourcePosition position) =>
        new(kind, _text[start.._pos].ToString(), null, position);

    private readonly void Error(SourcePosition position, string message) =>
        _diagnostics.Add(new Diagnostic(position, message));
}
