using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using SqliteConnection connection = new("Data Source=:memory:");
connection.Open();
using BookstoreContext db = new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlite(connection).Options);
db.Database.EnsureCreated();

InstitutionUser grace = new() { Id = 7, FullName = "Grace Hopper" };
InstitutionUser alan = new() { Id = 420, FullName = "Alan Turing", ApproverId = 7 };
Order order = new() { Id = 44, PlacedById = 420, TotalAmount = 56_700m };
db.AddRange(grace, alan, order);

// Alan'ın onaycısı da biriminin satın alma müdürü de Grace
db.Add(new PurchaseApproval { OrderId = 44, ApproverId = 7, Step = "Approver", DecidedAt = DateTimeOffset.UtcNow });
try
{
    db.Add(new PurchaseApproval { OrderId = 44, ApproverId = 7, Step = "PurchasingManager", DecidedAt = DateTimeOffset.UtcNow });
    db.SaveChanges();
    Console.WriteLine($"Approvals: {db.Set<PurchaseApproval>().Count()}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

public sealed class InstitutionUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public int? ApproverId { get; set; }
    public List<Order> ApprovedOrders { get; } = [];
}

public sealed class Order
{
    public int Id { get; set; }
    public int PlacedById { get; set; }
    public decimal TotalAmount { get; set; }
    public List<InstitutionUser> Approvers { get; } = [];
}

// Association class: InstitutionUser (onaycı) ile Order arasındaki link'in kendi verisi
public sealed class PurchaseApproval
{
    public int OrderId { get; set; }
    public int ApproverId { get; set; }
    public string Step { get; set; } = "";
    public DateTimeOffset DecidedAt { get; set; }
}

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InstitutionUser>();
        b.Entity<Order>()
            .HasMany(o => o.Approvers)
            .WithMany(u => u.ApprovedOrders)
            .UsingEntity<PurchaseApproval>(
                j => j.HasOne<InstitutionUser>().WithMany().HasForeignKey(a => a.ApproverId),
                j => j.HasOne<Order>().WithMany().HasForeignKey(a => a.OrderId),
                j => j.HasKey(a => new { a.OrderId, a.ApproverId }));
    }
}
