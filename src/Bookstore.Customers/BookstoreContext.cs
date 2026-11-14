using Bookstore.Crypto.Fields;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Customers;

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options, FieldEncryptor encryptor) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.Property(c => c.Name).HasMaxLength(200);
            customer.Property(c => c.Phone)
                .HasConversion(new EncryptedStringConverter(encryptor, "Customer.Phone"));
            customer.Property(c => c.PhoneIndex).HasMaxLength(24);
            customer.HasIndex(c => c.PhoneIndex);
        });
    }
}
