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

    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder books = app.MapGroup("/books");
        books.MapGet("", ListBooks).WithName("listBooks");
        books.MapMethods("/{isbn}", ["GET", "HEAD"], GetBook).WithName("getBook");

        RouteGroupBuilder staff = app.MapGroup("/books").RequireAuthorization(p => p.RequireRole("StoreStaff"));
        staff.MapPost("", CreateBook).WithName("createBook");
        staff.MapPut("/{isbn}", ReplaceBook).WithName("replaceBook");
        staff.MapPatch("/{isbn}", PatchBook).WithName("updateBook");
        staff.MapDelete("/{isbn}", DeleteBook).WithName("deleteBook");
        return app;
    }

    private static Results<Ok<Page<BookResponse>>, ProblemHttpResult> ListBooks(
        CatalogData catalog, int? pageSize, string? pageToken) =>
        Paging.Paginate([.. catalog.All.Select(BookResponse.From)], pageSize, pageToken);

    private static Results<Ok<BookResponse>, ProblemHttpResult> GetBook(
        string isbn, CatalogData catalog, HttpResponse response)
    {
        if (catalog.Find(isbn) is not { } book)
        {
            return BookNotFound(isbn);
        }

        response.Headers.ETag = book.ETag;
        return TypedResults.Ok(BookResponse.From(book));
    }

    private static Results<Created<BookResponse>, ProblemHttpResult> CreateBook(
        CreateBookRequest request, CatalogData catalog, HttpResponse response)
    {
        if (catalog.Find(request.Isbn) is not null)
        {
            return TypedResults.Problem(statusCode: 409, title: "Book already exists",
                detail: $"A book with ISBN {request.Isbn} already exists. Use PUT /books/{request.Isbn} to replace it.");
        }

        if (UnsupportedCurrency(request.Price) is { } problem)
        {
            return problem;
        }

        Book book = new(request.Isbn, request.Title, request.Author, request.Category, request.Price.Amount,
            StockQuantity: 0, Supplier: "unassigned", request.Description);
        catalog.Add(book);
        response.Headers.ETag = book.ETag;
        return TypedResults.Created($"/books/{book.Isbn}", BookResponse.From(book));
    }

    private static Results<Ok<BookResponse>, Created<BookResponse>, ProblemHttpResult> ReplaceBook(
        string isbn, BookFields fields, CatalogData catalog, HttpRequest request, HttpResponse response)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(isbn, "^97[89][0-9]{10}$"))
        {
            return TypedResults.Problem(statusCode: 400, title: "Invalid ISBN",
                detail: $"{isbn} is not an ISBN-13 without dashes.");
        }

        if (UnsupportedCurrency(fields.Price) is { } currencyProblem)
        {
            return currencyProblem;
        }

        if (catalog.Find(isbn) is not { } current)
        {
            Book created = new(isbn, fields.Title, fields.Author, fields.Category, fields.Price.Amount,
                StockQuantity: 0, Supplier: "unassigned", fields.Description);
            catalog.Add(created);
            response.Headers.ETag = created.ETag;
            return TypedResults.Created($"/books/{isbn}", BookResponse.From(created));
        }

        if (PreconditionFailed(request, current) is { } preconditionProblem)
        {
            return preconditionProblem;
        }

        Book replaced = current with
        {
            Title = fields.Title, Author = fields.Author, Category = fields.Category, Price = fields.Price.Amount,
            Description = fields.Description, Version = current.Version + 1,
        };
        catalog.Replace(replaced);
        response.Headers.ETag = replaced.ETag;
        return TypedResults.Ok(BookResponse.From(replaced));
    }

    // RFC 7396 JSON Merge Patch on the writable fields of a book.
    private static async Task<Results<Ok<BookResponse>, ProblemHttpResult, ValidationProblem>> PatchBook(
        string isbn, CatalogData catalog, HttpRequest request, HttpResponse response,
        IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions)
    {
        if (request.ContentType?.Split(';')[0].Trim() != MergePatch)
        {
            response.Headers["Accept-Patch"] = MergePatch;
            return TypedResults.Problem(statusCode: 415, title: "Unsupported patch format",
                detail: $"PATCH /books/{{isbn}} accepts {MergePatch} only.");
        }

        if (catalog.Find(isbn) is not { } current)
        {
            return BookNotFound(isbn);
        }

        if (PreconditionFailed(request, current) is { } preconditionProblem)
        {
            return preconditionProblem;
        }

        if (await JsonNode.ParseAsync(request.Body) is not JsonObject patch)
        {
            return TypedResults.Problem(statusCode: 400, title: "Invalid patch document",
                detail: "A merge patch for a book must be a JSON object.");
        }

        if (patch.Select(p => p.Key).FirstOrDefault(k => !WritableFields.Contains(k)) is { } readOnly)
        {
            return TypedResults.Problem(statusCode: 400, title: "Field cannot be changed",
                detail: $"'{readOnly}' is read-only or unknown; writable fields: {string.Join(", ", WritableFields)}.");
        }

        JsonSerializerOptions options = jsonOptions.Value.SerializerOptions;
        BookFields currentFields = new(current.Title, current.Author, current.Category,
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
            Title = merged.Title, Author = merged.Author, Category = merged.Category, Price = merged.Price.Amount,
            Description = merged.Description, Version = current.Version + 1,
        };
        catalog.Replace(updated);
        response.Headers.ETag = updated.ETag;
        return TypedResults.Ok(BookResponse.From(updated));
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
            : TypedResults.Problem(statusCode: 412, title: "Precondition failed",
                detail: $"If-Match {ifMatch} does not match the current ETag {current.ETag}.");
    }

    private static ProblemHttpResult? UnsupportedCurrency(Money price) =>
        price.Currency == "TRY"
            ? null
            : TypedResults.Problem(statusCode: 422, title: "Unsupported currency",
                detail: $"Catalog prices are in TRY; {price.Currency} is not accepted.");

    private static ProblemHttpResult BookNotFound(string isbn) =>
        TypedResults.Problem(statusCode: 404, title: "Book not found",
            detail: $"No book with ISBN {isbn} exists in the catalog.");
}
