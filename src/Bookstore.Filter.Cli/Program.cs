using System.Globalization;
using System.Linq.Expressions;
using Bookstore.Catalog;
using Bookstore.Filter;
using Bookstore.Filter.Binding;
using Bookstore.Filter.CodeGen;
using Bookstore.Filter.Data;
using Bookstore.Filter.Evaluation;
using Bookstore.Filter.Syntax;
using Microsoft.EntityFrameworkCore;

if (args is not [string command, string filter, ..])
{
    Console.Error.WriteLine("usage: bsfilter tokens|ast|run|check|sql|sql-inline <filter>");
    Console.Error.WriteLine("       bsfilter nest <depth> rd|pratt   or   bsfilter chain <count>");
    return 2;
}

return command switch
{
    "tokens" => PrintTokens(filter),
    "ast" => PrintAst(filter),
    "run" => Run(filter),
    "check" => Check(filter),
    "sql" => Sql(filter, LiteralMode.Parameterized),
    "sql-inline" => Sql(filter, LiteralMode.Inline),
    "nest" => Nest(int.Parse(filter), args.ElementAtOrDefault(2) ?? "pratt"),
    "chain" => Chain(int.Parse(filter)),
    _ => Usage(command),
};

static int Usage(string command)
{
    Console.Error.WriteLine($"unknown command '{command}'");
    return 2;
}

static int PrintTokens(string filter)
{
    (List<Token> tokens, List<Diagnostic> diagnostics) = Lexer.Tokenize(filter);
    foreach (Token token in tokens)
    {
        string value = token.Value switch
        {
            null => "",
            string s => $"\"{s}\"",
            object v => v.ToString()!,
        };
        Console.WriteLine($"{token.Position,-6} {token.Kind,-15} {token.Text,-16} {value}");
    }
    return Report(filter, diagnostics);
}

static int PrintAst(string filter)
{
    ParseResult result = Parser.Parse(filter);
    Console.Write(AstPrinter.Print(result.Root));
    return Report(filter, result.Diagnostics);
}

static int Run(string filter)
{
    ParseResult result = Parser.Parse(filter);
    if (!result.Success)
        return Report(filter, result.Diagnostics);
    try
    {
        foreach (Book book in Interpreter.Filter(SampleBooks.All, result.Root))
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{book.Title,-22} {book.Author,-15} {book.Price,8:0.00} TRY  stock {book.Stock}"));
        return 0;
    }
    catch (FilterRuntimeException ex)
    {
        Console.WriteLine($"runtime error: {ex.Message}");
        return 1;
    }
}

// type checks the filter, then runs the compiled delegate over the in-memory sample books
static int Check(string filter)
{
    CompiledFilter compiled = FilterCompiler.Compile(filter);
    if (!compiled.Success)
        return Report(filter, compiled.Diagnostics);

    Console.WriteLine(compiled.Expression);
    Func<Book, bool> predicate = compiled.Expression!.Compile();
    foreach (Book book in SampleBooks.All.Where(predicate))
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{book.Title,-22} {book.Author,-15} {book.Price,8:0.00} TRY  stock {book.Stock}"));
    return 0;
}

// the same expression tree handed to EF Core: prints the SQL Server query without opening a connection
static int Sql(string filter, LiteralMode mode)
{
    CompiledFilter compiled = FilterCompiler.Compile(filter, mode);
    if (!compiled.Success)
        return Report(filter, compiled.Diagnostics);

    using BookstoreContext db = BookstoreContext.ForSqlServer();
    Console.WriteLine(db.Books.Where(compiled.Expression!).ToQueryString());
    return 0;
}

// builds "stock > 0 and stock > 0 and ..." with the given count and binds it WITHOUT the length limit
static int Chain(int count)
{
    string filter = string.Join(" and ", Enumerable.Repeat("stock > 0", count));
    ParseResult parsed = Parser.Parse(filter);
    Console.WriteLine($"chain: {filter.Length} chars, {parsed.Diagnostics.Count} parse error(s), AST depth {Depth(parsed.Root)}");
    BindResult bound = Binder.Bind(parsed.Root, SymbolTable.ForBooks);
    Console.WriteLine($"bound: {bound.Diagnostics.Count} error(s)");
    Console.WriteLine($"limited: {FilterCompiler.Compile(filter).Diagnostics[0]}");
    return 0;
}

static int Depth(Expr expr)
{
    // iterative on purpose: this method must not overflow on the trees it measures
    int max = 0;
    Stack<(Expr Node, int Depth)> stack = new([(expr, 1)]);
    while (stack.TryPop(out (Expr Node, int Depth) item))
    {
        max = Math.Max(max, item.Depth);
        Expr[] children = item.Node switch
        {
            BinaryExpr b => [b.Left, b.Right],
            ComparisonExpr c => [c.Left, c.Right],
            NotExpr n => [n.Operand],
            _ => [],
        };
        foreach (Expr child in children)
            stack.Push((child, item.Depth + 1));
    }
    return max;
}

// builds "((((... price < 300 ...))))" with the given depth, to compare the two parsers
static int Nest(int depth, string parser)
{
    string filter = new string('(', depth) + "price < 300" + new string(')', depth);
    if (parser == "rd")
    {
        Expr root = RecursiveDescentParser.Parse(filter);
        Console.WriteLine($"rd: parsed {filter.Length} chars into {root.GetType().Name}");
        return 0;
    }
    ParseResult result = Parser.Parse(filter);
    Console.WriteLine($"pratt: {result.Diagnostics.Count} diagnostic(s)");
    foreach (Diagnostic diagnostic in result.Diagnostics)
        Console.WriteLine(diagnostic);
    return result.Success ? 0 : 1;
}

// prints each error under the line it belongs to, with a caret at the column
static int Report(string filter, IReadOnlyList<Diagnostic> diagnostics)
{
    string[] lines = filter.Split('\n');
    foreach (Diagnostic diagnostic in diagnostics)
    {
        Console.WriteLine(lines[diagnostic.Position.Line - 1]);
        Console.WriteLine(new string(' ', diagnostic.Position.Column - 1) + "^ " + diagnostic);
    }
    return diagnostics.Count == 0 ? 0 : 1;
}
