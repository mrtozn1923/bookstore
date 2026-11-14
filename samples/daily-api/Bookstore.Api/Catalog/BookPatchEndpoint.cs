using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

namespace Bookstore.Api.Catalog;

public class BookPatch
{
    [Range(1, 10000)]
    public decimal Price { get; set; }

    public string Title { get; set; } = "";
}

public static class BookPatchEndpoint
{
    // İstemcinin değiştirebileceği path'ler; Title DTO'da olsa da burada yok
    private static readonly string[] AllowedPaths = ["/price"];

    public static Results<Ok<Book>, NotFound, ValidationProblem> Patch(
        Isbn isbn, JsonPatchDocument<BookPatch> patch, BookCatalog catalog)
    {
        if (catalog.Find(isbn) is not { } book)
        {
            return TypedResults.NotFound();
        }

        Dictionary<string, string[]> errors = [];
        foreach (Operation<BookPatch> operation in patch.Operations)
        {
            if (operation.OperationType != OperationType.Replace
                || !AllowedPaths.Contains(operation.path, StringComparer.OrdinalIgnoreCase))
            {
                errors[operation.path] = [$"'{operation.op}' on '{operation.path}' is not allowed."];
            }
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        BookPatch target = new() { Price = book.Price, Title = book.Title };
        patch.ApplyTo(target, error => errors[error.Operation.path] = [error.ErrorMessage]);

        List<ValidationResult> results = [];
        Validator.TryValidateObject(target, new ValidationContext(target), results, validateAllProperties: true);
        foreach (ValidationResult result in results)
        {
            errors[result.MemberNames.First()] = [result.ErrorMessage!];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        Book updated = book with { Price = target.Price };
        catalog.Replace(updated);
        return TypedResults.Ok(updated);
    }
}
