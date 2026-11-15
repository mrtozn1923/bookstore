using Bookstore.Catalog;
using Bookstore.Filter.CodeGen;
using Bookstore.Filter.Data;
using Bookstore.Filter.Evaluation;
using Bookstore.Filter.Syntax;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Filter.Tests;

public class CodeGenTests
{
    public static TheoryData<string> Filters =>
    [
        "price < 300 and author = \"Jane Austen\"",
        "stock = 0 or price < 300 and year < 1900",
        "author contains \"Austen\" and not (price > 200)",
        "year >= 1900.5 or title contains \"Dune\"",
        "isbn = \"9780141439587\"",
    ];

    [Theory]
    [MemberData(nameof(Filters))]
    public void CompiledDelegate_AgreesWithInterpreter(string filter)
    {
        Func<Book, bool> predicate = FilterCompiler.Compile(filter).Expression!.Compile();
        IEnumerable<Book> interpreted = Interpreter.Filter(SampleBooks.All, Parser.Parse(filter).Root);

        Assert.Equal(interpreted, SampleBooks.All.Where(predicate));
    }

    [Fact]
    public void Sql_UsesParametersForUserValues()
    {
        using BookstoreContext db = BookstoreContext.ForSqlServer();
        string sql = db.Books.Where(FilterCompiler.Compile("author = \"x' OR 1=1 --\"").Expression!).ToQueryString();

        Assert.Contains("WHERE [b].[Author] = @value", sql);
        Assert.Contains("DECLARE @value nvarchar(100) = N'x'' OR 1=1 --';", sql);
    }

    [Fact]
    public void Sql_InlineMode_EscapesQuotes()
    {
        using BookstoreContext db = BookstoreContext.ForSqlServer();
        string sql = db.Books.Where(FilterCompiler.Compile("author = \"x' OR 1=1 --\"", LiteralMode.Inline).Expression!)
            .ToQueryString();

        Assert.EndsWith("WHERE [b].[Author] = N'x'' OR 1=1 --'", sql);
    }

    [Fact]
    public void Sql_Contains_EscapesLikeWildcards()
    {
        using BookstoreContext db = BookstoreContext.ForSqlServer();
        string sql = db.Books.Where(FilterCompiler.Compile("title contains \"50%_off\"").Expression!).ToQueryString();

        Assert.Contains(@"N'%50\%\_off%'", sql);
        Assert.Contains(@"LIKE @value_contains ESCAPE N'\'", sql);
    }

    [Fact]
    public void Compile_TooLongInput_IsRejectedBeforeParsing()
    {
        string filter = string.Join(" and ", Enumerable.Repeat("stock > 0", 200));

        CompiledFilter compiled = FilterCompiler.Compile(filter);

        Assert.False(compiled.Success);
        Assert.Equal($"error at 1:1: filter is {filter.Length} characters long (limit 1000)", compiled.Diagnostics[0].ToString());
    }
}
