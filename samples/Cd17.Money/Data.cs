using Microsoft.EntityFrameworkCore;

namespace Bookstore.Billing.Money;

public class BillingInvoice
{
    public int Id { get; set; }
    public decimal LegacyAmount { get; set; }
    public Money Total { get; set; }
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<BillingInvoice> Invoices => Set<BillingInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BillingInvoice>().ToTable("Invoices").ComplexProperty(i => i.Total, money =>
        {
            money.Property(x => x.Amount).HasColumnName("TotalAmount").HasPrecision(19, 4);
            money.Property(x => x.Currency).HasColumnName("TotalCurrency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3).IsUnicode(false).IsFixedLength();
        });
    }
}
