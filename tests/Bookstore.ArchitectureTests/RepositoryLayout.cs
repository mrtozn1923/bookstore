using System.Xml.Linq;

namespace Bookstore.ArchitectureTests;

// Reads the .csproj files directly: the rules are about references, not about types.
public sealed record ProjectFile(string Name, string FullPath, string Folder)
{
    public IEnumerable<string> ProjectReferences =>
        XDocument.Load(FullPath).Descendants("ProjectReference")
            .Select(r => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FullPath)!,
                r.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))));

    public IEnumerable<string> PackageReferences =>
        XDocument.Load(FullPath).Descendants("PackageReference").Select(p => p.Attribute("Include")!.Value);

    public override string ToString() => Name;
}

public static class RepositoryLayout
{
    public static readonly string Root = FindRoot();

    public static string Src(params string[] parts) => Path.Combine([Root, "src", .. parts]);

    // Every project under src/Services/<Service>/, together with its service folder.
    public static IEnumerable<ProjectFile> ServiceProjects() =>
        Directory.EnumerateDirectories(Src("Services"))
            .SelectMany(serviceFolder => Directory
                .EnumerateFiles(serviceFolder, "*.csproj", SearchOption.AllDirectories)
                .Select(path => new ProjectFile(Path.GetFileNameWithoutExtension(path), path, serviceFolder)));

    public static IEnumerable<ProjectFile> BuildingBlocks() =>
        Directory.EnumerateFiles(Src("BuildingBlocks"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => new ProjectFile(Path.GetFileNameWithoutExtension(path), path, Src("BuildingBlocks")));

    private static string FindRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Bookstore.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Bookstore.slnx not found above the test output folder.");
    }
}
