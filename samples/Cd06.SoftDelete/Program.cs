using Bookstore.Institutional.SoftDelete;
using Microsoft.EntityFrameworkCore;

string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options;

// Institution 1 = Sample School, institution 2 = Sample Library
await using (var setup = new BookstoreContext(options, institutionId: 1))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    Console.WriteLine(setup.Database.GenerateCreateScript()
        .Split('\n').First(l => l.Contains("UX_InstitutionUsers_Email_Active")).Trim());
    await setup.Database.ExecuteSqlRawAsync(
        "CREATE VIEW dbo.ActiveInstitutionUsers AS SELECT * FROM dbo.InstitutionUsers WHERE DeletedAt IS NULL");

    var ada = new InstitutionUser { InstitutionId = 1, InstitutionUnitId = 7, Email = "ada@example.com", FullName = "Ada Lovelace", Phone = "+90 555 000 00 01" };
    ada.PurchaseApprovals.Add(new PurchaseApproval { InstitutionId = 1, OrderId = 101, Status = ApprovalStatus.Approved });
    ada.PurchaseApprovals.Add(new PurchaseApproval { InstitutionId = 1, OrderId = 102, Status = ApprovalStatus.Pending });
    setup.InstitutionUsers.Add(ada);
    setup.InstitutionUsers.Add(new InstitutionUser { InstitutionId = 1, InstitutionUnitId = 7, Email = "alan@example.com", FullName = "Alan Turing" });
    setup.InstitutionUsers.Add(new InstitutionUser { InstitutionId = 2, InstitutionUnitId = 12, Email = "grace@example.com", FullName = "Grace Hopper" });
    await setup.SaveChangesAsync();
}

var clock = new FixedClock(new DateTimeOffset(2026, 3, 31, 17, 0, 0, TimeSpan.Zero));
await using var db = new BookstoreContext(options, institutionId: 1);
var lifecycle = new InstitutionUserLifecycle(db, clock);

Guid deletionId = await lifecycle.SoftDeleteAsync(1, "grace");
Console.WriteLine($"Soft deleted, deletion id: {deletionId}");

IQueryable<InstitutionUser> trash = db.InstitutionUsers
    .IgnoreQueryFilters([BookstoreContext.SoftDeleteFilter])
    .Where(u => u.DeletedAt != null);
Console.WriteLine(trash.ToQueryString());
foreach (InstitutionUser u in await trash.AsNoTracking().ToListAsync())
    Console.WriteLine($"Trash: {u.FullName} deleted {u.DeletedAt:yyyy-MM-dd HH:mm} by {u.DeletedBy}");

int visible = await db.PurchaseApprovals.CountAsync(a => a.RequestedById == 1);
Console.WriteLine($"Visible purchase approvals: {visible}");

// Paths that skip the query filter
int active = await db.InstitutionUsers.CountAsync();
int raw = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM dbo.InstitutionUsers").SingleAsync();
int view = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM dbo.ActiveInstitutionUsers").SingleAsync();
Console.WriteLine($"EF: {active}, raw SQL on table: {raw}, raw SQL on view: {view}");

db.InstitutionUsers.Add(new InstitutionUser { InstitutionId = 1, InstitutionUnitId = 7, Email = "ada@example.com", FullName = "Ada Lovelace" });
await db.SaveChangesAsync();
Console.WriteLine("Rehired with the same email: OK");

RestoreResult restore = await lifecycle.RestoreAsync(deletionId);
Console.WriteLine($"Restore: {restore}");

await lifecycle.EraseAsync(1);
InstitutionUser erased = await db.InstitutionUsers.IgnoreQueryFilters([BookstoreContext.SoftDeleteFilter])
    .AsNoTracking().SingleAsync(u => u.Id == 1);
Console.WriteLine($"Erased: {erased.FullName}, {erased.Email}, Phone={erased.Phone ?? "NULL"}");
