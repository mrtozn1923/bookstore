using Bookstore.Institutional.Attachments;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options;

await using (var setup = new BookstoreContext(options))
{
    await setup.Database.EnsureDeletedAsync();
    try
    {
        await setup.Database.EnsureCreatedAsync();
    }
    catch (SqlException ex)
    {
        Console.WriteLine($"Schema failed: {ex.Number} {ex.Message}");
        return;
    }

    // Sample School (institution 1)
    var ada = new InstitutionUser { InstitutionId = 1, FullName = "Ada Lovelace", UserName = "ada" };
    var alan = new InstitutionUser { InstitutionId = 1, FullName = "Alan Turing", UserName = "alan" };
    var grace = new InstitutionUser { InstitutionId = 1, FullName = "Grace Hopper", UserName = "grace" };
    setup.InstitutionUsers.AddRange(ada, alan, grace);
    await setup.SaveChangesAsync();

    var order = new Order { InstitutionId = 1, OrderNumber = "ORD-2026-0108", PlacedById = ada.Id };
    var invoice = new Invoice { InstitutionId = 1, InvoiceNumber = "INV-2026-0042", Order = order };
    // one at a time, so the attachment ids follow this order
    foreach (Attachment a in new[]
    {
        new Attachment { FileName = "quote.pdf", Order = order },
        new Attachment { FileName = "authorization-letter.pdf", InstitutionUser = ada },
        new Attachment { FileName = "inv-0042.pdf", Invoice = invoice }
    })
    {
        setup.Attachments.Add(a);
        await setup.SaveChangesAsync();
    }
}

await using var db = new BookstoreContext(options);

try
{
    db.Attachments.Add(new Attachment { FileName = "orphan.pdf" });
    await db.SaveChangesAsync();
}
catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
{
    Console.WriteLine($"Rejected: {sql.Number} {sql.Message}");
    db.ChangeTracker.Clear();
}

var query = db.Attachments
    .OrderBy(a => a.Id)
    .Select(a => new
    {
        a.Id,
        a.FileName,
        Owner = a.OrderId != null ? "Order " + a.Order!.OrderNumber
              : a.InvoiceId != null ? "Invoice " + a.Invoice!.InvoiceNumber
              : "InstitutionUser " + a.InstitutionUser!.FullName
    });
Console.WriteLine(query.ToQueryString());
foreach (var row in await query.ToListAsync())
{
    Console.WriteLine($"  #{row.Id} {row.FileName,-25} {row.Owner}");
}

var access = new AttachmentAccess(db);
UserInfo[] users =
[
    new(2, "alan", 1, ["InstitutionApprover"]),
    new(3, "grace", 1, ["PortalAdmin"]),
    new(1, "ada", 1, ["InstitutionBuyer"])
];
foreach (int attachmentId in new[] { 2, 3 })
{
    foreach (UserInfo u in users)
    {
        bool ok = await access.CanReadAsync(attachmentId, u);
        Console.WriteLine($"Attachment {attachmentId} for {u.Name,-5}: {(ok ? "allowed" : "denied")}");
    }
}

await db.Invoices.Where(i => i.InvoiceNumber == "INV-2026-0042").ExecuteDeleteAsync();
Console.WriteLine($"Attachments after deleting the invoice: {await db.Attachments.CountAsync()}");
