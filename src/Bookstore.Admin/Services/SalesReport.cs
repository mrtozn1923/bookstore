using Bookstore.Data;
using Bookstore.Ordering;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Admin.Services;

public sealed record CategorySales(string Category, string Slug, int Copies);

public sealed class SalesReport(IDbContextFactory<BookstoreContext> dbFactory)
{
    // Copies sold per category; cancelled orders don't count.
    public async Task<List<CategorySales>> GetCopiesByCategoryAsync(CancellationToken ct = default)
    {
        await using BookstoreContext db = await dbFactory.CreateDbContextAsync(ct);
        return await (
            from item in db.Set<OrderItem>()
            join order in db.Orders on item.OrderId equals order.Id
            join book in db.Books on item.BookId equals book.Id
            where order.Status != OrderStatus.Cancelled
            group item by new { book.Category.Name, book.Category.Slug } into category
            orderby category.Key.Name
            select new CategorySales(category.Key.Name, category.Key.Slug, category.Sum(i => i.Quantity)))
            .ToListAsync(ct);
    }
}
