using Bookstore.Filter.Binding;
using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Tests;

public class BinderTests
{
    private static BindResult Bind(string text) => Binder.Bind(Parser.Parse(text).Root, SymbolTable.ForBooks);

    [Theory]
    [InlineData("price = \"abc\"", "error at 1:7: operator '=' cannot be applied to decimal and string")]
    [InlineData("title < \"M\"", "error at 1:7: operator '<' cannot be applied to string and string")]
    [InlineData("stock contains \"1\"", "error at 1:7: operator 'contains' cannot be applied to int and string")]
    [InlineData("prise < 300", "error at 1:1: unknown field 'prise'; did you mean 'price'?")]
    [InlineData("publisher = \"Penguin\"", "error at 1:1: unknown field 'publisher'")]
    [InlineData("price", "error at 1:1: a filter must be true or false, but this one is decimal")]
    public void Bind_InvalidFilter_ReportsTypeError(string text, string expected)
    {
        Assert.Equal(expected, Assert.Single(Bind(text).Diagnostics).ToString());
    }

    [Fact]
    public void Bind_UnknownField_DoesNotCascade()
    {
        // one mistake, one message: the comparison that uses the bad name stays quiet
        Assert.Single(Bind("prise < 300 and stock > 0").Diagnostics);
    }

    [Fact]
    public void Bind_IntLiteralComparedWithDecimalField_IsFoldedToDecimal()
    {
        BoundComparison comparison = Assert.IsType<BoundComparison>(Bind("price < 300").Root);

        Assert.Equal(new BoundLiteral(300m, FilterType.Decimal), comparison.Right);
    }

    [Fact]
    public void Bind_IntFieldComparedWithDecimalLiteral_GetsConversionNode()
    {
        BoundComparison comparison = Assert.IsType<BoundComparison>(Bind("year >= 1900.5").Root);

        BoundConversion conversion = Assert.IsType<BoundConversion>(comparison.Left);
        Assert.Equal(FilterType.Int, conversion.Operand.Type);
    }

    [Fact]
    public void Bind_FieldNames_AreCaseInsensitive()
    {
        Assert.True(Bind("PRICE < 300 AND Author = \"Emma\"").Success);
    }
}
