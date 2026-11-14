using System.Text.Json;
using System.Text.Json.Nodes;
using Bookstore.Api.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Catalog;

public static class BookEndpoints
{
    private const string MergePatch = "application/merge-patch+json";
    private static readonly string[] WritableFields = ["title", "author", "category", "price", "description"];
    private static readonly string[] ListParameters =
        ["category", "author", "minPrice", "maxPrice", "inStock", "q", "sort", "fields", "pageSize", "pageToken"];
    private static readonly string[] SelectableFields =
        ["isbn", "title", "authors", "category", "price", "inStock", "description"];
    private static readonly SortField<Book>[] SortFields =
        [new("title", b => b.Title), new("author", b => b.AuthorLine), new("price", b => b.Price)];
    private static readonly SortField<Book> ByIsbn = new("isbn", b => b.Isbn);

    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/books");
        books.MapGet("", ListBooks).WithName("listBooks");
        // RFC 10008 QUERY: safe and idempotent like GET, but the criteria travel in the body.
        books.MapMethods("", ["QUERY"], LookupBooks).WithName("lookupBooks");
        books.MapMethods("/{isbn}", ["GET", "HEAD"], GetBook).WithName("getBook");

        RouteGroupBuilder staff = app.MapGroup("/books").RequireAuthorization(p => p.RequireRole("StoreStaff"));
        staff.MapPost("", CreateBook).WithName("createBook");
        staff.MapPut("/{isbn}", ReplaceBook).WithName("replaceBook");
        staff.MapPatch("/{isbn}", PatchBook).WithName("updateBook");
        staff.MapDelete("/{isbn}", DeleteBook).WithName("deleteBook");
        return app;
    }

    private static Results<Ok<Page<JsonObject>>, ProblemHttpResult> ListBooks(
        HttpContext http, CatalogData catalog, Paging paging, IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> json,
        string? category, string? author, decimal? minPrice, decimal? maxPrice, bool? inStock, string? q,
        string? sort, string? fields, int? pageSize, string? pageToken)
    {
        if (Paging.RejectUnknownParameters(http.Request, ListParameters) is { } unknown)
        {
            return unknown;
        }

        if (Paging.ParseSort(sort ?? "title", SortFields, out List<SortSpec<Book>> order) is { } badSort)
        {
            return badSort;
        }

        ApiVersion version = http.Current();
        string[] selectable = version == ApiVersions.V20260901 ? [.. SelectableFields, "author"] : SelectableFields;
        string[] selected = fields?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? selectable;
        if (selected.Any(f => !selectable.Contains(f)))
        {
            return Paging.Invalid("fields", "unknown-field");
        }

        IEnumerable<Book> books = catalog.All.Where(b =>
            (category is null || b.Category == category) &&
            (author is null || b.Authors.Contains(author, StringComparer.OrdinalIgnoreCase)) &&
            (minPrice is null || b.Price >= minPrice) &&
            (maxPrice is null || b.Price <= maxPrice) &&
            (inStock is null || b.StockQuantity > 0 == inStock) &&
            (q is null || b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.AuthorLine.Contains(q, StringComparison.OrdinalIgnoreCase)));

        // The token is bound to everything that changes the sequence; pageSize and fields may change between pages.
        string query = string.Join('|', category, author, minPrice, maxPrice, inStock, q);
        JsonSerializerOptions options = json.Value.SerializerOptions;
        return paging.Paginate(http, books, order, ByIsbn, query, pageSize, pageToken, book =>
        {
            JsonObject item = JsonSerializer.SerializeToNode(BookResponse.From(book, version), options)!.AsObject();
            foreach (string name in item.Select(p => p.Key).Where(k => !selected.Contains(k)).ToList())
            {
                item.Remove(name);
            }

            return item;
        });
    }

    // A school checks up to 500 ISBNs at once; that list does not fit in a URL.
    private static Results<Ok<Page<BookResponse>>, ValidationProblem> LookupBooks(
        BookLookupRequest request, CatalogData catalog, HttpContext http) =>
        request.Isbns.Length is 0 or > 500
            ? TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["isbns"] = ["out-of-range"] })
            : TypedResults.Ok(new Page<BookResponse>(
                [.. request.Isbns.Distinct().Select(catalog.Find).OfType<Book>().Select(b => BookResponse.From(b, http.Current()))], null));

    private static Results<Ok<BookResponse>, ProblemHttpResult> GetBook(
        string isbn, CatalogData catalog, HttpContext http, HttpResponse response)
    {
        if (catalog.Find(isbn) is not { } book)
        {
            return BookNotFound(isbn);
        }

        response.Headers.ETag = book.ETag;
        return TypedResults.Ok(BookResponse.From(book, http.Current()));
    }

    private static Results<Created<BookResponse>, ProblemHttpResult> CreateBook(
        CreateBookRequest request, CatalogData catalog, HttpContext http, HttpResponse response)
    {
        if (catalog.Find(request.Isbn) is not null)
        {
            return ProblemCatalog.Create(ProblemCatalog.BookAlreadyExists, [request.Isbn]);
        }

        if (UnsupportedCurrency(request.Price) is { } problem)
        {
            return problem;
        }

        Book book = new(request.Isbn, request.Title, [request.Author], request.Category, request.Price.Amount,
            StockQuantity: 0, Supplier: "unassigned", request.Description);
        catalog.Add(book);
        response.Headers.ETag = book.ETag;
        return TypedResults.Created($"/books/{book.Isbn}", BookResponse.From(book, http.Current()));
    }

    private static Results<Ok<BookResponse>, Created<BookResponse>, ProblemHttpResult, ValidationProblem> ReplaceBook(
        string isbn, BookFields fields, CatalogData catalog, HttpContext http, HttpRequest request, HttpResponse response)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(isbn, "^97[89][0-9]{10}$"))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["isbn"] = ["invalid-format"] });
        }

        if (UnsupportedCurrency(fields.Price) is { } currencyProblem)
        {
            return currencyProblem;
        }

        if (catalog.Find(isbn) is not { } current)
        {
            Book created = new(isbn, fields.Title, [fields.Author], fields.Category, fields.Price.Amount,
                StockQuantity: 0, Supplier: "unassigned", fields.Description);
            catalog.Add(created);
            response.Headers.ETag = created.ETag;
            return TypedResults.Created($"/books/{isbn}", BookResponse.From(created, http.Current()));
        }

        if (PreconditionFailed(request, current) is { } preconditionProblem)
        {
            return preconditionProblem;
        }

        Book replaced = current with
        {
            Title = fields.Title, Authors = KeepAuthors(current, fields.Author), Category = fields.Category,
            Price = fields.Price.Amount,
            Description = fields.Description, Version = current.Version + 1,
        };
        catalog.Replace(replaced);
        response.Headers.ETag = replaced.ETag;
        return TypedResults.Ok(BookResponse.From(replaced, http.Current()));
    }

    // RFC 7396 JSON Merge Patch on the writable fields of a book.
    private static async Task<Results<Ok<BookResponse>, ProblemHttpResult, ValidationProblem>> PatchBook(
        string isbn, CatalogData catalog, HttpContext http, HttpRequest request, HttpResponse response,
        IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions)
    {
        if (request.ContentType?.Split(';')[0].Trim() != MergePatch)
        {
            response.Headers["Accept-Patch"] = MergePatch;
            return ProblemCatalog.Create(ProblemCatalog.UnsupportedMediaType, [MergePatch]);
        }

        if (catalog.Find(isbn) is not { } current)
        {
            return BookNotFound(isbn);
        }

        if (PreconditionFailed(request, current) is { } preconditionProblem)
        {
            return preconditionProblem;
        }

        JsonNode? document;
        try
        {
            document = await JsonNode.ParseAsync(request.Body);
        }
        catch (JsonException)
        {
            return ProblemCatalog.Create(ProblemCatalog.MalformedRequest);
        }

        if (document is not JsonObject patch)
        {
            return ProblemCatalog.Create(ProblemCatalog.MalformedRequest);
        }

        if (patch.Select(p => p.Key).FirstOrDefault(k => !WritableFields.Contains(k)) is { } readOnly)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [readOnly] = ["read-only"] });
        }

        JsonSerializerOptions options = jsonOptions.Value.SerializerOptions;
        BookFields currentFields = new(current.Title, current.AuthorLine, current.Category,
            new Money(current.Price, "TRY"), current.Description);
        JsonNode target = JsonSerializer.SerializeToNode(currentFields, options)!;
        BookFields merged = MergePatchDocument.Apply(target, patch).Deserialize<BookFields>(options)!;

        if (MiniValidation.Validate(merged) is { Count: > 0 } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (UnsupportedCurrency(merged.Price) is { } currencyProblem)
        {
            return currencyProblem;
        }

        Book updated = current with
        {
            Title = merged.Title, Authors = KeepAuthors(current, merged.Author), Category = merged.Category,
            Price = merged.Price.Amount,
            Description = merged.Description, Version = current.Version + 1,
        };
        catalog.Replace(updated);
        response.Headers.ETag = updated.ETag;
        return TypedResults.Ok(BookResponse.From(updated, http.Current()));
    }

    private static Results<NoContent, ProblemHttpResult> DeleteBook(
        string isbn, CatalogData catalog, HttpRequest request)
    {
        if (catalog.Find(isbn) is not { } current)
        {
            return BookNotFound(isbn);
        }

        if (PreconditionFailed(request, current) is { } preconditionProblem)
        {
            return preconditionProblem;
        }

        catalog.Remove(isbn);
        return TypedResults.NoContent();
    }

    // If-Match is optional here; post 07 makes it required (428) for every write.
    private static ProblemHttpResult? PreconditionFailed(HttpRequest request, Book current)
    {
        string? ifMatch = request.Headers.IfMatch;
        return ifMatch is null || ifMatch == "*" || ifMatch == current.ETag
            ? null
            : ProblemCatalog.Create(ProblemCatalog.PreconditionFailed, [ifMatch, current.ETag]);
    }

    // Writes still take a single author string; an unchanged string keeps every co-author.
    private static IReadOnlyList<string> KeepAuthors(Book current, string author) =>
        author == current.AuthorLine ? current.Authors : [author];

    private static ProblemHttpResult? UnsupportedCurrency(Money price) =>
        price.Currency == "TRY"
            ? null
            : ProblemCatalog.Create(ProblemCatalog.UnsupportedCurrency, [price.Currency]);

    private static ProblemHttpResult BookNotFound(string isbn) =>
        ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/books/{isbn}"]);
}
