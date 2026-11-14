namespace Bookstore.Tools;

public sealed class ImportOptions
{
    public const string SectionName = "Import";

    public string File { get; set; } = "data/books.csv";
    public int BatchSize { get; set; } = 2;
    public int DelayMs { get; set; } = 1000;
}
