using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string code = """
    using System.Linq;

    record Book(string Title, string Author, decimal Price, int Year);

    static class Queries
    {
        static object Cheap(Book[] books) =>
            books.Where(b => b.Price < 300 && b.Author == "Jane Austen");

        static object Broken(Book[] books) =>
            books.Where(b => b.Price == "abc");
    }
    """;

// 1. syntax only: the parser knows the shape, not the meaning
SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
Console.WriteLine($"syntax errors: {tree.GetDiagnostics().Count()}");

// 2. a compilation adds references; the semantic model answers "what does this name mean?"
IEnumerable<MetadataReference> references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path));
CSharpCompilation compilation = CSharpCompilation.Create("Demo", [tree], references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
SemanticModel model = compilation.GetSemanticModel(tree);

foreach (BinaryExpressionSyntax binary in tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>()
             .Where(b => !b.IsKind(SyntaxKind.LogicalAndExpression)))
{
    string op = model.GetSymbolInfo(binary).Symbol?.ToDisplayString() ?? "(no operator found)";
    TypeInfo right = model.GetTypeInfo(binary.Right);
    Conversion conversion = model.GetConversion(binary.Right);
    string kind = conversion.IsIdentity ? "identity" : conversion.IsNumeric ? "implicit numeric" : "none";
    Console.WriteLine($"{binary,-26} {op}");
    Console.WriteLine($"{"",-26} right operand: {right.Type} -> {right.ConvertedType} ({kind})");
}

MemberAccessExpressionSyntax price = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>()
    .First(m => m.Name.Identifier.Text == "Price");
Console.WriteLine($"b.Price binds to: {model.GetSymbolInfo(price).Symbol?.ToDisplayString()} ({model.GetSymbolInfo(price).Symbol?.Kind})");

// 3. the binder's errors only appear once there is a compilation; formatted in English on any machine
foreach (Diagnostic diagnostic in compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    Console.WriteLine(CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture));
