using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api;

public sealed class UniqueIsbnAttribute : AsyncValidationAttribute
{
    // Kural yalnızca async çalışır; senkron yol bilerek hata verir
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        throw new InvalidOperationException("UniqueIsbn only validates asynchronously.");

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        IBookRepository books = validationContext.GetRequiredService<IBookRepository>();
        if (value is string isbn && await books.IsbnExistsAsync(isbn, cancellationToken))
        {
            return new ValidationResult($"ISBN '{isbn}' is already in use.", [validationContext.MemberName!]);
        }
        return ValidationResult.Success;
    }
}

public interface IBookRepository
{
    Task<bool> IsbnExistsAsync(string isbn, CancellationToken cancellationToken);
}

public class InMemoryBookRepository : IBookRepository
{
    private readonly HashSet<string> _isbns = ["978-0441013593", "978-0141439587"];

    public async Task<bool> IsbnExistsAsync(string isbn, CancellationToken cancellationToken)
    {
        // Gerçek bir veritabanı sorgusunu taklit ediyoruz
        await Task.Delay(50, cancellationToken);
        return _isbns.Contains(isbn);
    }
}
