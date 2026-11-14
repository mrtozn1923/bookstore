using Microsoft.EntityFrameworkCore;

namespace Bookstore.Payments.Data;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(payment =>
        {
            payment.HasIndex(p => p.OrderId).IsUnique();
            payment.HasIndex(p => p.ProviderChargeId);
            payment.HasIndex(p => new { p.Status, p.UpdatedAtUtc });
            payment.Property(p => p.Amount).HasPrecision(12, 2);
            payment.Property(p => p.Currency).HasMaxLength(3).IsUnicode(false);
            payment.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            payment.Property(p => p.ProviderChargeId).HasMaxLength(64).IsUnicode(false);
            payment.Property(p => p.FailureReason).HasMaxLength(200);
            payment.Property<byte[]>("RowVersion").IsRowVersion();
            payment.Ignore(p => p.IdempotencyKey);
            payment.Ignore(p => p.IsFinal);
        });
        modelBuilder.Entity<ProcessedWebhook>(webhook =>
        {
            webhook.HasKey(w => w.Id);
            webhook.Property(w => w.Id).HasMaxLength(64).IsUnicode(false);
        });
    }
}
