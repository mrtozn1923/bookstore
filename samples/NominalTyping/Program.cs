using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// The same shape as a TypeScript structural-typing example, compiled by Roslyn.
const string Code = """
    public sealed record Book(string Isbn, string Title, decimal Price);
    public sealed record BookDto(string Isbn, string Title, decimal Price);

    public static class Shipping
    {
        public static decimal WithShipping(Book book) => book.Price + 25m;

        public static decimal FromApi(BookDto dto) => WithShipping(dto);

        public static int Twice() => "3" * 2;
    }
    """;

IEnumerable<MetadataReference> references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path));
CSharpCompilation compilation = CSharpCompilation.Create("Typing", [CSharpSyntaxTree.ParseText(Code)], references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

foreach (Diagnostic diagnostic in compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    Console.WriteLine(CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture));
