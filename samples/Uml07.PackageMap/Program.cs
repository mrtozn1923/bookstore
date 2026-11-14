using System.Reflection;
using System.Xml.Linq;

// dotnet run -- map src            → .csproj'lardan PlantUML package diagram
// dotnet run -- check <bin klasörü> → derlenmiş assembly'lerde katman kuralı
return args switch
{
    ["map", string srcDir] => Print(PackageDiagram(srcDir)),
    ["check", string binDir] => Check(binDir),
    _ => Print("usage: map <src> | check <bin>", 2)
};

static int Print(string text, int exitCode = 0)
{
    Console.WriteLine(text);
    return exitCode;
}

static string PackageDiagram(string srcDir)
{
    List<string> lines = ["@startuml", "set separator none", "skinparam packageStyle folder"];
    List<string> deps = [];
    SortedSet<string> nugets = [];
    foreach (string csproj in Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories).Order())
    {
        string name = Path.GetFileNameWithoutExtension(csproj);
        lines.Add($"package \"{name}\" as {Alias(name)} {{\n}}");
        XDocument doc = XDocument.Load(csproj);
        foreach (XElement r in doc.Descendants("ProjectReference"))
        {
            string target = Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/'));
            deps.Add($"{Alias(name)} ..> {Alias(target)}");
        }
        foreach (XElement p in doc.Descendants("PackageReference"))
        {
            string package = p.Attribute("Include")!.Value;
            nugets.Add(package);
            deps.Add($"{Alias(name)} ..> {Alias(package)}");
        }
    }
    lines.AddRange(nugets.Select(n => $"package \"{n}\" as {Alias(n)} <<NuGet>> {{\n}}"));
    return string.Join('\n', [.. lines, .. deps, "@enduml"]);
}

static string Alias(string name) => name.Replace("Bookstore.", "").Replace("Microsoft.", "").Replace(".", "");

static int Check(string binDir)
{
    Dictionary<string, string[]> allowed = new()
    {
        ["Bookstore.Institutional.Domain"] = ["System."],
        ["Bookstore.Institutional.Application"] = ["System.", "Bookstore.Institutional.Domain"],
    };
    int violations = 0;
    foreach ((string assembly, string[] prefixes) in allowed)
    {
        Assembly asm = Assembly.LoadFrom(Path.Combine(binDir, assembly + ".dll"));
        foreach (AssemblyName dep in asm.GetReferencedAssemblies())
        {
            bool ok = prefixes.Any(p => dep.Name!.StartsWith(p, StringComparison.Ordinal));
            Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {assembly} -> {dep.Name}");
            if (!ok) violations++;
        }
    }
    Console.WriteLine($"{violations} violation(s)");
    return violations == 0 ? 0 : 1;
}
