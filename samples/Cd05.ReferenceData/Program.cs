using Bookstore.Institutional.ReferenceData;
using Microsoft.EntityFrameworkCore;

string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options;
string mode = args.FirstOrDefault() ?? "web";

switch (mode)
{
    case "naive":
        // The rows in the database did not change; only the code did
        int[] stored = [(int)StatusV1.Approved, (int)StatusV1.Rejected];
        foreach (int value in stored)
            Console.WriteLine($"stored {value}: v1 reads {(StatusV1)value}, v2 reads {(StatusV2)value}");
        return;

    case "renamed":
    {
        var legacyOptions = new DbContextOptionsBuilder<LegacyContext>().UseSqlServer(cs).Options;
        await using var legacy = new LegacyContext(legacyOptions);
        await legacy.Database.EnsureDeletedAsync();
        await legacy.Database.EnsureCreatedAsync();
        // A row written while the member was still called "Cancelled"
        await legacy.Database.ExecuteSqlRawAsync("INSERT INTO LegacyOrders (Status) VALUES (N'Cancelled')");
        try
        {
            await legacy.LegacyOrders.ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
        await legacy.Database.EnsureDeletedAsync();
        return;
    }

    case "script-cascade":
    {
        await using var db = new BookstoreContext(options, restrict: false);
        Console.WriteLine(db.Database.GenerateCreateScript());
        return;
    }

    case "script":
    {
        await using var db = new BookstoreContext(options);
        Console.WriteLine(db.Database.GenerateCreateScript());
        return;
    }

    case "setup":
    {
        await using var db = new BookstoreContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        db.BudgetCategories.AddRange(
            new BudgetCategory { Code = "library-books", NameTr = "Kütüphane kitapları", NameEn = "Library books", SortOrder = 10 },
            new BudgetCategory { Code = "classroom-sets", NameTr = "Sınıf setleri", NameEn = "Classroom sets", SortOrder = 20 },
            new BudgetCategory { Code = "teacher-resources", NameTr = "Öğretmen kaynakları", NameEn = "Teacher resources", SortOrder = 30 });
        await db.SaveChangesAsync();
        db.Orders.AddRange(
            new Order { Status = OrderStatus.Approved, BudgetCategoryId = 1, TotalAmount = 6850.50m },
            new Order { Status = OrderStatus.Approved, BudgetCategoryId = 2, TotalAmount = 4200m },
            new Order { Status = OrderStatus.PendingApproval, BudgetCategoryId = 1, TotalAmount = 4200m },
            new Order { Status = OrderStatus.Draft, BudgetCategoryId = 3, TotalAmount = 310m });
        await db.SaveChangesAsync();
        Console.WriteLine("seeded");
        return;
    }

    case "verify":
    {
        await using var db = new BookstoreContext(options);
        await LookupCheck.VerifyAsync(db, Console.Out);
        return;
    }
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(cs));
WebApplication app = builder.Build();

app.MapGet("/api/lookups/budget-categories", async (string? lang, BookstoreContext db) =>
    await db.BudgetCategories
        .Where(c => c.IsActive)
        .OrderBy(c => c.SortOrder)
        .Select(c => new { c.Id, c.Code, Name = lang == "tr" ? c.NameTr : c.NameEn })
        .ToListAsync());

app.MapPost("/api/admin/budget-categories", async (BudgetCategory body, BookstoreContext db) =>
{
    db.BudgetCategories.Add(body);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/budget-categories/{body.Id}", new { body.Id, body.Code });
});

// No delete: deactivate, so old orders keep working
app.MapPost("/api/admin/budget-categories/{id:int}/deactivate", async (int id, BookstoreContext db) =>
    await db.BudgetCategories.Where(c => c.Id == id)
        .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false)) == 1
        ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/orders/{id:int}", async (int id, string? lang, BookstoreContext db) =>
    await (from o in db.Orders
           join s in db.OrderStatuses on o.Status equals s.Id
           join c in db.BudgetCategories on o.BudgetCategoryId equals c.Id
           where o.Id == id
           select new
           {
               o.Id,
               Status = s.Code,
               StatusName = lang == "tr" ? s.NameTr : s.NameEn,
               BudgetCategory = lang == "tr" ? c.NameTr : c.NameEn,
               BudgetCategoryActive = c.IsActive
           }).SingleOrDefaultAsync() is { } order
        ? Results.Ok(order) : Results.NotFound());

app.Run("http://localhost:5305");
