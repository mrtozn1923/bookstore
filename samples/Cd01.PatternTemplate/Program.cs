using Bookstore.Institutional;
using Microsoft.EntityFrameworkCore;

// Post 01: unique e-mail + soft delete. Usage: dotnet run -- [naive|filtered|script]
string mode = args.FirstOrDefault() ?? "filtered";
string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options;

await using BookstoreContext db = mode == "naive" ? new NaiveContext(options) : new BookstoreContext(options);

if (mode == "script")
{
    Console.WriteLine(db.Database.GenerateCreateScript());
    return;
}

await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();

var school = new Institution { Name = "Sample School", TaxNumber = "1234567890" };
var science = new InstitutionUnit { Code = "SCI", Name = "Science" };
school.Units.Add(science);
db.Institutions.Add(school);
await db.SaveChangesAsync();   // Sample School and its Science unit (Id 1)

var ada = new InstitutionUser
{
    InstitutionUnitId = 1, FullName = "Ada Lovelace", Email = "ada@example.com",
    JoinedOn = new DateOnly(2024, 3, 1), SpendingLimit = 85_000m
};
db.InstitutionUsers.Add(ada);
await db.SaveChangesAsync();

// Ada leaves: the row is not deleted, it is flagged
ada.IsDeleted = true;
await db.SaveChangesAsync();

// Three months later Ada comes back: the portal admin creates a new user
db.InstitutionUsers.Add(new InstitutionUser
{
    InstitutionUnitId = 1, FullName = "Ada Lovelace", Email = "ada@example.com",
    JoinedOn = new DateOnly(2026, 9, 1), SpendingLimit = 92_000m
});
try
{
    await db.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    Console.WriteLine(ex.InnerException?.Message);
    return;
}

foreach (InstitutionUser u in await db.InstitutionUsers.OrderBy(u => u.Id).ToListAsync())
    Console.WriteLine($"{u.Id} {u.Email} deleted={u.IsDeleted} joined={u.JoinedOn:yyyy-MM-dd}");

// The naive model: the unique index knows nothing about IsDeleted
sealed class NaiveContext(DbContextOptions<BookstoreContext> options) : BookstoreContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<InstitutionUser>().HasIndex(u => u.Email).IsUnique().HasFilter(null);
    }
}
