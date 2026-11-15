using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bookstore.Mcp;

// All three tools are read-only: ReadOnly = true shows up as annotations.readOnlyHint in tools/list.
[McpServerToolType]
public sealed class CatalogTools(BookstoreApi api)
{
    [McpServerTool(Name = "search_books", ReadOnly = true, Idempotent = true)]
    [Description("Searches the Bookstore catalog by title, author or ISBN-13. Returns up to 10 books as 'isbn | title | author | price | year'.")]
    public async Task<string> SearchBooks(
        [Description("Words from the title or author name, or an ISBN-13, e.g. 'Austen'")] string query,
        [Description("Optional category slug, e.g. 'classics' or 'science-fiction'")] string? category = null,
        CancellationToken ct = default)
    {
        PagedResult<BookSummary> result = await api.SearchBooksAsync(query, category, ct);
        if (result.Items.Count == 0)
        {
            return $"No books match '{query}'. Try a shorter query or remove the category.";
        }
        IEnumerable<string> lines = result.Items.Select(b =>
            FormattableString.Invariant($"{b.Isbn} | {b.Title} | {b.Author} | {b.Price:0.00} TRY | {b.PublishedYear}"));
        return $"{result.Total} book(s):\n" + string.Join("\n", lines);
    }

    [McpServerTool(Name = "get_book", ReadOnly = true, Idempotent = true)]
    [Description("Returns one book by ISBN-13 with its description and the number of copies available in stock.")]
    public async Task<string> GetBook(
        [Description("ISBN-13 without dashes, e.g. 9780441013593")] string isbn,
        CancellationToken ct = default)
    {
        BookDetails? book = await api.GetBookAsync(isbn, ct);
        return book is null
            ? $"No book with ISBN {isbn}. Use search_books to find the ISBN first."
            : FormattableString.Invariant(
                $"{book.Title} by {book.Author} ({book.PublishedYear}), {book.Category}, {book.Price:0.00} TRY, {book.Available} in stock.\n{book.Description}");
    }

    [McpServerTool(Name = "list_institutions", ReadOnly = true, Idempotent = true)]
    [Description("Lists the institutions (schools, libraries, companies) that can place institutional orders, with their codes.")]
    public async Task<string> ListInstitutions(CancellationToken ct = default)
    {
        IReadOnlyList<InstitutionView> institutions = await api.GetInstitutionsAsync(ct);
        return string.Join("\n", institutions.Select(i => $"{i.Code} | {i.Name} | {i.Kind}"));
    }
}
