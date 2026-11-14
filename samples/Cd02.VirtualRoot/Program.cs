using Bookstore.Institutional;
using Cd02.VirtualRoot;
using Microsoft.EntityFrameworkCore;

string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

if (args.Contains("--opening"))
{
    // Guess First: 0 travels as a plain int
    await using var db = new BookstoreContext(new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options);
    await Seed.ResetAsync(db);

    int unitId = 0; // "Sample School" was selected in the Angular tree

    int count = await db.InstitutionUsers.Where(u => u.InstitutionUnitId == unitId).CountAsync();
    Console.WriteLine($"Users in unit 0: {count}");

    db.InstitutionUsers.Add(new InstitutionUser { InstitutionUnitId = unitId, FullName = "Edsger Dijkstra", Email = "edsger@example.com" });
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException ex)
    {
        Console.WriteLine(ex.InnerException?.Message);
    }
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(cs));
builder.Services.AddProblemDetails();
var app = builder.Build();

if (args.Contains("--seed"))
{
    using IServiceScope scope = app.Services.CreateScope();
    await Seed.ResetAsync(scope.ServiceProvider.GetRequiredService<BookstoreContext>());
}

app.MapGet("/api/units/tree", async (BookstoreContext db) =>
{
    string institution = await db.Institutions.Select(i => i.Name).SingleAsync();
    List<UnitRow> rows = await db.InstitutionUnits
        .Select(u => new UnitRow(u.Id, u.ParentId, u.Name)).ToListAsync();
    return UnitTreeBuilder.Build(institution, rows);
});

app.MapGet("/api/institution-users", async (InstitutionUnitScope unitId, BookstoreContext db) =>
    await db.InstitutionUsers.InScope(unitId)
        .OrderBy(u => u.FullName)
        .Select(u => new { u.Id, u.FullName, u.InstitutionUnitId })
        .ToListAsync());

app.MapPost("/api/institution-users", async (CreateInstitutionUser body, BookstoreContext db) =>
{
    // No virtual root on the write side: a user belongs to a real unit
    if (body.UnitId == InstitutionUnitScope.InstitutionRootId)
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["unitId"] = ["0 is the virtual institution root. Choose a real unit."]
        });

    if (!await db.InstitutionUnits.AnyAsync(u => u.Id == body.UnitId))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["unitId"] = [$"Unit {body.UnitId} does not exist."]
        });

    var user = new InstitutionUser { InstitutionUnitId = body.UnitId, FullName = body.FullName, Email = body.Email };
    db.InstitutionUsers.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/institution-users/{user.Id}", new { user.Id, user.InstitutionUnitId });
});

app.Run();

public sealed record CreateInstitutionUser(int UnitId, string FullName, string Email);
