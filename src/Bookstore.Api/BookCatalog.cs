using System.Globalization;
using Microsoft.Extensions.Options;

namespace Bookstore.Api;

public sealed record Book(string Isbn, string Title, string Author, decimal PriceTry);

public sealed record BookView(string Isbn, string Title, string ShelfLabel, decimal PriceTry);

public sealed class BookCatalog(IOptions<StoreOptions> options)
{
    private static readonly Book[] Books =
    [
        new("9780441172719", "Dune", "Frank Herbert", 420.00m),
        new("9789750719387", "İnce Memed", "Yaşar Kemal", 310.00m),
        new("9789750738609", "Çalıkuşu", "Reşat Nuri Güntekin", 240.00m),
        new("9789754700114", "Saatleri Ayarlama Enstitüsü", "Ahmet Hamdi Tanpınar", 280.00m),
        new("9780141439587", "Emma", "Jane Austen", 180.00m),
        new("9789750722752", "Kürk Mantolu Madonna", "Sabahattin Ali", 150.00m),
    ];

    public IReadOnlyList<BookView> SortedByTitle()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(options.Value.Culture);
        StringComparer comparer = StringComparer.Create(culture, ignoreCase: false);

        return [.. Books
            .OrderBy(b => b.Title, comparer)
            .Select(b => new BookView(b.Isbn, b.Title, b.Title.ToUpper(culture), b.PriceTry))];
    }
}
