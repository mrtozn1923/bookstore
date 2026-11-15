using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory;

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<StockItem> StockItems => Set<StockItem>();
}
