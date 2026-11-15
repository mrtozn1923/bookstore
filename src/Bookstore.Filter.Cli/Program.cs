using System.Globalization;
using Bookstore.Catalog;
using Bookstore.Filter.Evaluation;
using Bookstore.Filter.Syntax;

if (args is not [string command, string filter, ..])
{
    Console.Error.WriteLine("usage: bsfilter tokens|ast|run <filter>  or  bsfilter nest <depth> rd|pratt");
    return 2;
}

return command switch
{
    "tokens" => PrintTokens(filter),
    "ast" => PrintAst(filter),
    "run" => Run(filter),
    "nest" => Nest(int.Parse(filter), args.ElementAtOrDefault(2) ?? "pratt"),
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
