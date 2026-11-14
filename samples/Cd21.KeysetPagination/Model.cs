using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering.Paging;

// dbo.Orders is created and filled by sql/cd21-01-orders.sql (2 million rows)
public class Order
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int InstitutionUnitId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public byte Status { get; set; }
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.CreatedAt).HasColumnType("datetime2(0)");
            e.Property(o => o.ApprovedAt).HasColumnType("datetime2(0)");
            e.Property(o => o.TotalAmount).HasPrecision(19, 4);
            e.HasIndex(o => new { o.InstitutionId, o.CreatedAt, o.Id })
                .IsDescending(false, true, true)
                .HasDatabaseName("IX_Orders_Institution_CreatedAt_Id")
                .IncludeProperties(o => o.TotalAmount);
        });
    }
}
