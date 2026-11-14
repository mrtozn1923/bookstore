using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Attachments;

public class Order
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string OrderNumber { get; set; } = "";
    public int PlacedById { get; set; }   // InstitutionUser
}

public class Invoice
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
}

public class InstitutionUser
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string FullName { get; set; } = "";
    public string UserName { get; set; } = "";
}

public class Attachment
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public int? InstitutionUserId { get; set; }
    public InstitutionUser? InstitutionUser { get; set; }
}

public sealed record UserInfo(int Id, string Name, int InstitutionId, string[] Roles);

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
        => c.Properties<string>().HaveMaxLength(100);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // an invoice's order must not be deleted: Restrict (Cascade here causes multiple cascade paths)
        modelBuilder.Entity<Invoice>().HasOne(i => i.Order).WithMany().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attachment>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(200);
            e.ToTable(t => t.HasCheckConstraint("CK_Attachments_ExactlyOneOwner",
                "(CASE WHEN [OrderId] IS NULL THEN 0 ELSE 1 END"
                + " + CASE WHEN [InvoiceId] IS NULL THEN 0 ELSE 1 END"
                + " + CASE WHEN [InstitutionUserId] IS NULL THEN 0 ELSE 1 END) = 1"));
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InstitutionUser).WithMany().HasForeignKey(x => x.InstitutionUserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
