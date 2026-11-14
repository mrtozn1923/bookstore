using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using SqliteConnection connection = new("Data Source=:memory:");
connection.Open();
using BookstoreContext db = new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlite(connection).Options);
db.Database.EnsureCreated();

string script = db.Database.GenerateCreateScript();
int start = script.IndexOf("CREATE TABLE \"Orders\"");
Console.WriteLine(script[start..(script.IndexOf(");", start) + 2)]);
foreach (string line in script.Split('\n').Where(l => l.Contains("FK_PurchaseApprovals") || l.Contains("FK_InstitutionUsers")))
    Console.WriteLine(line.Trim());

InstitutionUser grace = new() { Id = 7, FullName = "Grace Hopper" };
InstitutionUser alan = new() { Id = 420, FullName = "Alan Turing", ApproverId = 7 };
Order order = new()
{
    Id = 44, PlacedById = 420, Quantity = 150,
    Total = new Money(56_700m, "TRY"),
    Attachment = new OrderAttachment { FileName = "donation-letter.pdf", ContentType = "application/pdf" }
};
order.Approvals.Add(new PurchaseApproval { ApproverId = 7, Step = ApprovalStep.Approver, DecidedAt = DateTimeOffset.UtcNow });
order.Approvals.Add(new PurchaseApproval { ApproverId = 7, Step = ApprovalStep.PurchasingManager, DecidedAt = DateTimeOffset.UtcNow });
db.AddRange(grace, alan, order);
db.SaveChanges();
Console.WriteLine($"Approvals saved: {db.PurchaseApprovals.Count()}");

// Composition: sipariş silinince onaylar da silinir
db.Orders.Remove(order);
db.SaveChanges();
Console.WriteLine($"After deleting order 44: approvals = {db.PurchaseApprovals.Count()}");

// Association: siparişi olan bir kullanıcı silinemez
db.Orders.Add(new Order { Id = 45, PlacedById = 420, Quantity = 20, Total = new Money(6_840m, "TRY") });
db.SaveChanges();
db.ChangeTracker.Clear();
try
{
    db.InstitutionUsers.Remove(new InstitutionUser { Id = 420 });
    db.SaveChanges();
}
catch (DbUpdateException ex)
{
    Console.WriteLine(ex.InnerException?.Message);
}

public enum ApprovalStep { Approver, PurchasingManager }

public readonly record struct Money(decimal Amount, string Currency);

public sealed class OrderAttachment
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
}

public sealed class InstitutionUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public int? ApproverId { get; set; }
    public InstitutionUser? Approver { get; set; }
}

public sealed class Order
{
    public int Id { get; set; }
    public int PlacedById { get; set; }
    public InstitutionUser PlacedBy { get; set; } = null!;
    public int Quantity { get; set; }
    public Money Total { get; set; }
    public OrderAttachment? Attachment { get; set; }
    public List<PurchaseApproval> Approvals { get; } = [];
}

public sealed class PurchaseApproval
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ApproverId { get; set; }
    public InstitutionUser Approver { get; set; } = null!;
    public ApprovalStep Step { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Self-association: approver 0..1, requesters 0..*
        b.Entity<InstitutionUser>().HasOne(u => u.Approver).WithMany().HasForeignKey(u => u.ApproverId);

        b.Entity<Order>(o =>
        {
            // Association: sipariş bir kullanıcıya ait ama kullanıcının parçası değil
            o.HasOne(x => x.PlacedBy).WithMany().HasForeignKey(x => x.PlacedById).OnDelete(DeleteBehavior.Restrict);
            // «dataType» değer olarak aynı tabloda
            o.ComplexProperty(x => x.Total);
            // Composition [0..1]: ek dosya siparişin parçası
            o.OwnsOne(x => x.Attachment);
            // Composition [0..*] {ordered}: onaylar siparişin parçası
            o.HasMany(x => x.Approvals).WithOne().HasForeignKey(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PurchaseApproval>(a =>
        {
            a.HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApproverId).OnDelete(DeleteBehavior.Restrict);
            // Her basamakta tek karar; aynı kişi iki basamakta onaylayabilir
            a.HasIndex(x => new { x.OrderId, x.Step }).IsUnique();
        });
    }
}
