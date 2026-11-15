using Bookstore.Institutional;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Privacy;
using Bookstore.Privacy.Retention;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data;

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<CustomerSession> CustomerSessions => Set<CustomerSession>();
    public DbSet<ErasureRequest> ErasureRequests => Set<ErasureRequest>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<DestructionLogEntry> DestructionLog => Set<DestructionLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(b =>
        {
            b.HasIndex(c => c.Email).IsUnique();
            b.HasMany(c => c.Orders).WithOne(o => o.Customer).HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasMany(c => c.Sessions).WithOne().HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Order>().HasIndex(o => o.BillingNationalIdIndex);
    }
}
