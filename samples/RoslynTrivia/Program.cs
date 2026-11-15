using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

string code = """
    // cheap books only
    var cheap=books.Where( b=>b.Price<300m );   /* TRY */

    """;

foreach (SyntaxToken token in SyntaxFactory.ParseTokens(code))
{
    string leading = string.Join(",", token.LeadingTrivia.Select(t => t.Kind()));
    string trailing = string.Join(",", token.TrailingTrivia.Select(t => t.Kind()));
    Console.WriteLine($"{token.Kind(),-22} {token.Text,-8} leading=[{leading}] trailing=[{trailing}]");
}

SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
SyntaxNode root = tree.GetRoot();
Console.WriteLine($"round-trip equal: {root.ToFullString() == code}");
Console.WriteLine("normalized:");
Console.WriteLine(root.NormalizeWhitespace().ToFullString());
