using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Tests;

public class ParserTests
{
    private static Expr ParseOk(string text)
    {
        ParseResult result = Parser.Parse(text);
        Assert.Empty(result.Diagnostics);
        return result.Root;
    }

    [Fact]
    public void Parse_AndBindsTighterThanOr()
    {
        Expr root = ParseOk("stock = 0 or price < 300 and year < 1900");

        BinaryExpr or = Assert.IsType<BinaryExpr>(root);
        Assert.Equal(LogicalOperator.Or, or.Operator);
        BinaryExpr and = Assert.IsType<BinaryExpr>(or.Right);
        Assert.Equal(LogicalOperator.And, and.Operator);
    }

    [Fact]
    public void Parse_AndIsLeftAssociative()
    {
        Expr root = ParseOk("stock > 0 and price < 300 and year < 1900");

        BinaryExpr outer = Assert.IsType<BinaryExpr>(root);
        Assert.IsType<BinaryExpr>(outer.Left);
        Assert.IsType<ComparisonExpr>(outer.Right);
    }

    [Fact]
    public void Parse_NotAppliesToTheWholeComparison()
    {
        NotExpr not = Assert.IsType<NotExpr>(ParseOk("not stock = 0"));
        Assert.IsType<ComparisonExpr>(not.Operand);
    }

    [Fact]
    public void Parse_ParenthesesOverridePrecedence()
    {
        BinaryExpr and = Assert.IsType<BinaryExpr>(ParseOk("(stock = 0 or price < 300) and year < 1900"));
        Assert.Equal(LogicalOperator.And, and.Operator);
        Assert.Equal(LogicalOperator.Or, Assert.IsType<BinaryExpr>(and.Left).Operator);
    }

    [Theory]
    [InlineData("price < 300 and author = \"Jane Austen\"")]
    [InlineData("stock = 0 or price < 300 and year < 1900")]
    [InlineData("not not (title contains \"Dune\" or price >= 100.50) and stock != 0")]
    [InlineData("true")]
    public void Parse_PrattAndRecursiveDescent_ProduceTheSameAst(string text)
    {
        Assert.Equal(RecursiveDescentParser.Parse(text), ParseOk(text));
    }

    [Fact]
    public void Parse_ReportsSeveralErrorsInOnePass()
    {
        ParseResult result = Parser.Parse("price < and stock > or (title = \"Dune\"");

        Assert.Equal(
            [
                "error at 1:9: expected a field, a value or '(' but found 'and'",
                "error at 1:21: expected a field, a value or '(' but found 'or'",
                "error at 1:39: expected ')' to close '(' at 1:24",
            ],
            result.Diagnostics.Select(d => d.ToString()));
    }

    [Fact]
    public void Parse_ChainedComparison_IsAnError()
    {
        Diagnostic error = Assert.Single(Parser.Parse("price < 300 < 400").Diagnostics);
        Assert.Equal("error at 1:13: comparisons cannot be chained; combine them with 'and'", error.ToString());
    }

    [Fact]
    public void Parse_DeepNesting_StopsAtTheLimitInsteadOfOverflowingTheStack()
    {
        string text = new string('(', 100_000) + "price < 300" + new string(')', 100_000);

        Diagnostic error = Assert.Single(Parser.Parse(text).Diagnostics);
        Assert.Equal($"error at 1:{Parser.MaxDepth + 1}: filter is nested too deeply (limit {Parser.MaxDepth})", error.ToString());
    }

    [Fact]
    public void Parse_LexerErrors_DoNotCascadeIntoParserErrors()
    {
        Diagnostic error = Assert.Single(Parser.Parse("başlık = \"Dune\"").Diagnostics);
        Assert.Contains("must use ASCII letters only", error.Message);
    }
}
