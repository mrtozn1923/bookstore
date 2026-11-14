using System.Globalization;
using Bookstore.Institutional.Workflow;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options;
var clock = new FixedClock(new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero));

if (args.Contains("--api"))
{
    // dotnet run -- --api   (after a console run has created the demo data)
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(cs));
    builder.Services.AddSingleton<TimeProvider>(clock);
    builder.Services.AddScoped<OrderWorkflow>();
    builder.Services.AddAuthentication(DemoAuthentication.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DemoAuthentication>(DemoAuthentication.SchemeName, null);
    builder.Services.AddAuthorization();
    WebApplication app = builder.Build();
    app.UseAuthentication();
    app.MapOrderWorkflow();
    app.Run();
    return;
}

UserInfo ada = new("ada", ["InstitutionBuyer"]);
UserInfo grace = new("grace", ["InstitutionApprover"]);
UserInfo alan = new("alan", ["InstitutionApprover"]);

await using (var setup = new BookstoreContext(options))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    setup.Orders.AddRange(
        new Order { InstitutionId = 1, OrderNumber = "ORD-2026-0107", TotalAmount = 18_500m, Status = OrderStatus.Approved },
        new Order { InstitutionId = 1, OrderNumber = "ORD-2026-0108", TotalAmount = 6_850.50m, Status = OrderStatus.Draft });
    await setup.SaveChangesAsync();
}

// Naive edit: the form was opened yesterday, while ORD-2026-0107 was still a draft
var dto = new { Id = 1, TotalAmount = 18_900m, Status = OrderStatus.Draft };
await using (var db = new BookstoreContext(options))
{
    Order order = await db.Orders.SingleAsync(o => o.Id == dto.Id);
    OrderStatus before = order.Status;
    order.TotalAmount = dto.TotalAmount;
    order.Status = dto.Status;
    await db.SaveChangesAsync();
    Console.WriteLine($"Naive edit: {order.OrderNumber} {before} -> {order.Status}, no error, no history");
}

// Stateless: same rules in code
{
    var approved = new Order { Status = OrderStatus.Approved };
    var machine = StatelessOrder.Create(approved, grace);
    Console.WriteLine($"Stateless permitted from Approved: {string.Join(", ", await machine.GetPermittedTriggersAsync())}");
    try
    {
        machine.Fire("Revise");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"Stateless: {ex.Message}");
    }
}

await using (var db = new BookstoreContext(options))
{
    var wf = new OrderWorkflow(db, clock);
    Console.WriteLine($"Allowed for ada on order 2:   {string.Join(", ", await wf.AllowedActionsAsync(2, ada))}");
    Console.WriteLine($"Allowed for grace on order 2: {string.Join(", ", await wf.AllowedActionsAsync(2, grace))}");
    Order order = await db.Orders.SingleAsync(o => o.Id == 2);
    Console.WriteLine($"Submit by ada:    {await wf.ExecuteAsync(order, "Submit", ada, null)}");
    Console.WriteLine($"Approve by ada:   {await wf.ExecuteAsync(order, "Approve", ada, null)}");
    Console.WriteLine($"Reject by grace:  {await wf.ExecuteAsync(order, "Reject", grace, null)}");
    Console.WriteLine($"Allowed for grace on order 2: {string.Join(", ", await wf.AllowedActionsAsync(2, grace))}");
}

// Two approvers open the same order at the same time
await using (var graceDb = new BookstoreContext(options))
await using (var alanDb = new BookstoreContext(options))
{
    var graceWf = new OrderWorkflow(graceDb, clock);
    var alanWf = new OrderWorkflow(alanDb, clock);
    Order graceCopy = await graceDb.Orders.SingleAsync(o => o.Id == 2);
    Order alanCopy = await alanDb.Orders.SingleAsync(o => o.Id == 2);
    Console.WriteLine($"Approve by grace: {await graceWf.ExecuteAsync(graceCopy, "Approve", grace, null)}");
    Console.WriteLine($"Reject by alan:   {await alanWf.ExecuteAsync(alanCopy, "Reject", alan, "Budget frozen")}");
    Console.WriteLine($"Approve again:    {await graceWf.ExecuteAsync(graceCopy, "Approve", grace, null)}");
}

await using (var db = new BookstoreContext(options))
{
    Console.WriteLine("History of ORD-2026-0108:");
    foreach (StatusHistory h in await db.StatusHistory.Where(h => h.OrderId == 2).OrderBy(h => h.Id).ToListAsync())
    {
        Console.WriteLine($"  {h.ChangedAt:yyyy-MM-dd HH:mm} {h.ChangedBy,-6} {h.FromStatus,-16} -> {h.ToStatus}");
    }

    // the database says no as well: a transition that isn't in StatusTransitions
    try
    {
        db.StatusHistory.Add(new StatusHistory
        {
            OrderId = 2, FromStatus = OrderStatus.Draft, ToStatus = OrderStatus.Delivered,
            ChangedBy = "admin-panel", ChangedAt = clock.GetUtcNow()
        });
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
    {
        Console.WriteLine($"DB guard: {sql.Number} {sql.Message}");
    }
}
