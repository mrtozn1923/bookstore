namespace Bookstore.Filter.Syntax;

public enum TokenKind
{
    EndOfInput,
    Bad,

    Identifier,
    Number,
    String,

    // keywords
    And,
    Or,
    Not,
    Contains,
    True,
    False,

    // operators and punctuation
    Equal,
    NotEqual,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,
    OpenParen,
    CloseParen,
}
