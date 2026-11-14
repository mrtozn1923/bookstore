using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockReservation> Reservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockItem>(stock =>
        {
            stock.HasKey(s => s.Isbn);
            stock.Property(s => s.Isbn).HasMaxLength(13).IsUnicode(false);
            stock.Property(s => s.RowVersion).IsRowVersion();
            stock.Ignore(s => s.Available);
            stock.ToTable(t => t.HasCheckConstraint(
                "CK_StockItems_Quantities", "[QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]"));
        });

        modelBuilder.Entity<StockReservation>(reservation =>
        {
            reservation.ToTable("StockReservations");
            // One reservation per order: a retried ReserveStock call cannot reserve twice.
            reservation.HasIndex(r => r.OrderId).IsUnique();
            reservation.HasIndex(r => new { r.Status, r.ExpiresAtUtc });
            reservation.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            reservation.OwnsMany(r => r.Lines, line =>
            {
                line.ToTable("StockReservationLines");
                line.Property(l => l.Isbn).HasMaxLength(13).IsUnicode(false);
            });
        });
    }
}
