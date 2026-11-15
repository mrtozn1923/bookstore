using Bookstore.Filter.Syntax;

if (args is not [string command, string filter])
{
    Console.Error.WriteLine("usage: bsfilter tokens <filter>");
    return 2;
}

switch (command)
{
    case "tokens":
        return PrintTokens(filter);
    default:
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
    foreach (Diagnostic diagnostic in diagnostics)
        Console.WriteLine(diagnostic);
    return diagnostics.Count == 0 ? 0 : 1;
}
