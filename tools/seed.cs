#!/usr/bin/env dotnet
#:package System.CommandLine@3.0.0

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

Option<int> countOption = new("--count")
{
    Description = "Number of books to write",
    DefaultValueFactory = _ => 10,
};
Option<FileInfo> outputOption = new("--output")
{
    Description = "Seed file to write",
    DefaultValueFactory = _ => new FileInfo(Path.Combine("data", "books.json")),
};
Option<bool> resetOption = new("--reset")
{
    Description = "Delete the seed file before writing",
};

RootCommand root = new("Writes seed data for Bookstore's in-memory catalog.")
{
    countOption,
    outputOption,
    resetOption,
};

root.SetAction(result =>
{
    FileInfo output = result.GetValue(outputOption)!;
    if (result.GetValue(resetOption) && output.Exists)
    {
        output.Delete();
        Console.WriteLine($"Deleted {output.Name}");
    }

    (string Title, string Author)[] classics =
    [
        ("Dune", "Frank Herbert"),
        ("Emma", "Jane Austen"),
        ("The Hobbit", "J. R. R. Tolkien"),
        ("Middlemarch", "George Eliot"),
        ("Moby-Dick", "Herman Melville"),
    ];

    Random random = new(42);
    List<SeedBook> books = [.. Enumerable.Range(1, result.GetValue(countOption)).Select(i =>
    {
        (string title, string author) = classics[(i - 1) % classics.Length];
        return new SeedBook(Isbn13("978000000" + i.ToString("D3", CultureInfo.InvariantCulture)), title, author, random.Next(150, 450) + 0.90m);
    })];

    output.Directory!.Create();
    File.WriteAllText(output.FullName, JsonSerializer.Serialize(books, SeedJson.Default.ListSeedBook));
    Console.WriteLine($"Wrote {books.Count} books to {Path.GetRelativePath(Environment.CurrentDirectory, output.FullName)}");
});

return root.Parse(args).Invoke();

static string Isbn13(string first12)
{
    int sum = first12.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
    return first12 + ((10 - sum % 10) % 10);
}

internal sealed record SeedBook(string Isbn, string Title, string Author, decimal Price);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(List<SeedBook>))]
internal sealed partial class SeedJson : JsonSerializerContext;
