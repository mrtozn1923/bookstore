using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Bookstore.Catalog;

namespace Bookstore.Filter.Tests;

// Native AOT keeps only the members it can see at compile time; the generated tree must name its operator methods.
public class AotTests
{
    [Theory]
    [InlineData("price < 300", "op_LessThan")]
    [InlineData("price >= 300.5", "op_GreaterThanOrEqual")]
    [InlineData("author = \"Jane Austen\"", "op_Equality")]
    [InlineData("title != \"Emma\"", "op_Inequality")]
    public void Comparisons_name_their_operator_method(string filter, string method)
    {
        Expression<Func<Book, bool>> expression = FilterCompiler.Compile(filter).Expression!;

        BinaryExpression comparison = Assert.IsAssignableFrom<BinaryExpression>(expression.Body);
        Assert.Equal(method, comparison.Method?.Name);
    }

    [Fact]
    public void Interpreted_delegate_returns_the_same_books()
    {
        Expression<Func<Book, bool>> expression = FilterCompiler.Compile("price < 300 and author = \"Jane Austen\"").Expression!;

        // the interpreter is what Compile() falls back to when RuntimeFeature.IsDynamicCodeSupported is false
        Func<Book, bool> interpreted = expression.Compile(preferInterpretation: true);

        Assert.True(RuntimeFeature.IsDynamicCodeSupported);
        Assert.Equal(SampleBooks.All.Where(expression.Compile()), SampleBooks.All.Where(interpreted));
    }
}
