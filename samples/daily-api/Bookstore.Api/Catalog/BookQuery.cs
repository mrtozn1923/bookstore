using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Catalog;

public enum BookSort
{
    Title,
    Price,
    PriceDescending,
}

public record BookQuery(
    BookCatalog Catalog,
    string? Category,
    [FromQuery(Name = "q")] string? Search,
    [property: Range(1, 50)] int PageSize = 10,
    int Page = 1,
    BookSort Sort = BookSort.Title)
{
    public IEnumerable<Book> Apply()
    {
        IEnumerable<Book> books = Catalog.All;
        if (Category is not null)
        {
            books = books.Where(b => b.Category == Category);
        }
        if (Search is not null)
        {
            books = books.Where(b => b.Title.Contains(Search, StringComparison.OrdinalIgnoreCase));
        }

        books = Sort switch
        {
            BookSort.Price => books.OrderBy(b => b.Price),
            BookSort.PriceDescending => books.OrderByDescending(b => b.Price),
            _ => books.OrderBy(b => b.Title),
        };
        return books.Skip((Page - 1) * PageSize).Take(PageSize);
    }
}
