using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bookstore.Institutional.OneToOne;

public class Order
{
    public int Id { get; set; }
    public string Status { get; set; } = "PendingApprover";
    public PurchaseApproval? Approval { get; set; }
}

public class PurchaseApproval
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public required string Approver { get; set; }
    public required string Step { get; set; }
}

public sealed class OrderContext(string connectionString, List<string> sql) : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) => options
        .UseSqlServer(connectionString)
        .LogTo(sql.Add, [RelationalEventId.CommandExecuted]);

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Beyaz tahtadaki model: bir siparişin en fazla bir onayı var
        b.Entity<Order>().HasOne(o => o.Approval).WithOne()
            .HasForeignKey<PurchaseApproval>(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
