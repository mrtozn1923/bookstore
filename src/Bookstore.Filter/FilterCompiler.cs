using System.Linq.Expressions;
using Bookstore.Catalog;
using Bookstore.Filter.Binding;
using Bookstore.Filter.CodeGen;
using Bookstore.Filter.Syntax;

namespace Bookstore.Filter;

public sealed record CompiledFilter(Expression<Func<Book, bool>>? Expression, IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool Success => Expression is not null;
}

// The whole pipeline for text that comes from a user: limits -> lex/parse -> bind/type check -> code generation.
public static class FilterCompiler
{
    // a filter typed into a search box; long enough for real use, short enough to bound the tree size
    public const int MaxLength = 1_000;

    public static CompiledFilter Compile(string text, LiteralMode mode = LiteralMode.Parameterized)
    {
        if (text.Length > MaxLength)
            return Fail(new Diagnostic(new SourcePosition(1, 1), $"filter is {text.Length} characters long (limit {MaxLength})"));

        ParseResult parsed = Parser.Parse(text);
        if (!parsed.Success)
            return new CompiledFilter(null, parsed.Diagnostics);

        BindResult bound = Binder.Bind(parsed.Root, SymbolTable.ForBooks);
        if (!bound.Success)
            return new CompiledFilter(null, bound.Diagnostics);

        return new CompiledFilter(ExpressionCompiler.Compile(bound.Root, mode), []);
    }

    private static CompiledFilter Fail(Diagnostic diagnostic) => new(null, [diagnostic]);
}
