using System.Globalization;
using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Tests;

public class LexerTests
{
    private static TokenKind[] Kinds(string text) =>
        [.. Lexer.Tokenize(text).Tokens.Select(t => t.Kind)];

    [Fact]
    public void Tokenize_SimpleFilter_ProducesExpectedKinds()
    {
        Assert.Equal(
            [TokenKind.Identifier, TokenKind.Less, TokenKind.Number, TokenKind.And,
             TokenKind.Identifier, TokenKind.Equal, TokenKind.String, TokenKind.EndOfInput],
            Kinds("price < 300 and author = \"Jane Austen\""));
    }

    [Theory]
    [InlineData("price<=300", TokenKind.LessOrEqual)]
    [InlineData("price>=300", TokenKind.GreaterOrEqual)]
    [InlineData("price!=300", TokenKind.NotEqual)]
    [InlineData("price<300", TokenKind.Less)]
    public void Tokenize_Operators_UseMaximalMunch(string text, TokenKind expected)
    {
        Assert.Equal([TokenKind.Identifier, expected, TokenKind.Number, TokenKind.EndOfInput], Kinds(text));
    }

    [Fact]
    public void Tokenize_Keywords_AreCaseInsensitive_EvenUnderTurkishCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal([TokenKind.Identifier, TokenKind.Contains, TokenKind.String, TokenKind.EndOfInput],
                Kinds("TITLE CONTAINS \"Dune\""));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ToLower_UnderTurkishCulture_ChangesTheLetterI()
    {
        CultureInfo turkish = new("tr-TR");
        Assert.Equal("contaıns", "CONTAINS".ToLower(turkish));
        Assert.Equal("contains", "CONTAINS".ToLowerInvariant());
    }

    [Fact]
    public void Tokenize_Number_IsParsedWithInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Token number = Lexer.Tokenize("49.90").Tokens[0];
            Assert.Equal(49.90m, number.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Tokenize_StringEscapes_AreDecoded()
    {
        Token token = Lexer.Tokenize("""title = "The \"Hobbit\" \\ draft" """).Tokens[2];
        Assert.Equal("The \"Hobbit\" \\ draft", token.Value);
    }

    [Fact]
    public void Tokenize_BackslashAtEndOfLine_EndsTheStringOnThatLine()
    {
        (List<Token> tokens, List<Diagnostic> diagnostics) = Lexer.Tokenize("title = \"Dune\\\nand stock > 0");

        Assert.Equal("error at 1:9: unterminated string literal", Assert.Single(diagnostics).ToString());
        Assert.Equal(new SourcePosition(2, 1), tokens[3].Position);
    }

    [Fact]
    public void Tokenize_TracksLineAndColumn_AcrossCommentsAndNewlines()
    {
        List<Token> tokens = Lexer.Tokenize("# cheap books\nprice < 300\n  and stock > 0").Tokens;
        Assert.Equal(new SourcePosition(2, 1), tokens[0].Position);
        Assert.Equal(new SourcePosition(3, 3), tokens[3].Position);
        Assert.Equal(new SourcePosition(3, 13), tokens[5].Position);
    }

    [Fact]
    public void Tokenize_UnterminatedString_ReportsColumnOfOpeningQuote()
    {
        Diagnostic error = Assert.Single(Lexer.Tokenize("title = \"Dune").Diagnostics);
        Assert.Equal("error at 1:9: unterminated string literal", error.ToString());
    }

    [Theory]
    [InlineData("başlık = \"Dune\"", "error at 1:1: field name 'başlık' must use ASCII letters only")]
    [InlineData("price < 300 € ", "error at 1:13: unexpected character '€' (U+20AC)")]
    [InlineData("price ! 300", "error at 1:7: unexpected character '!' (U+0021) (did you mean '!='?)")]
    [InlineData("price < 300.", "error at 1:13: expected a digit after '.'")]
    public void Tokenize_BadInput_ReportsReadableError(string text, string expected)
    {
        Assert.Equal(expected, Lexer.Tokenize(text).Diagnostics[0].ToString());
    }

    [Fact]
    public void Tokenize_Garbage_AlwaysTerminates()
    {
        Random random = new(42);
        for (int i = 0; i < 1_000; i++)
        {
            char[] chars = [.. Enumerable.Range(0, 50).Select(_ => (char)random.Next(0, 0xD7FF))];
            List<Token> tokens = Lexer.Tokenize(chars).Tokens;
            Assert.Equal(TokenKind.EndOfInput, tokens[^1].Kind);
            Assert.True(tokens.Count <= chars.Length + 1);
        }
    }
}
