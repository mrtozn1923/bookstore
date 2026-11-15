using System.Runtime.InteropServices;
using Bookstore.Api.Data;
using Bookstore.E2E.Host;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

// Playwright'ın webServer'ı bu process'i başlatır: SQL Server container'ı + Kestrel üzerinde API
const int ApiPort = 5280;

// WebApplicationFactory, API'nin content root'unu bulduğu MvcTestingAppManifest.json'u
// çalışma klasöründe arar; dotnet run ise çalışma klasörünü değiştirmez
Environment.CurrentDirectory = AppContext.BaseDirectory;

await using MsSqlContainer sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();
await sql.StartAsync();
string connectionString = new SqlConnectionStringBuilder(sql.GetConnectionString())
{
    InitialCatalog = "BookstoreDb"
}.ConnectionString;

// Şema ve başlangıç verisi API dinlemeye başlamadan önce hazır olmalı
DbContextOptions<BookstoreDbContext> options = new DbContextOptionsBuilder<BookstoreDbContext>()
    .UseSqlServer(connectionString)
    .Options;
await using (BookstoreDbContext db = new(options))
{
    await db.Database.MigrateAsync();
    db.Books.AddRange(
        new Book { Isbn = "978-0441172719", Title = "Dune", Price = 64.90m, Stock = 7 },
        new Book { Isbn = "978-0141439587", Title = "Emma", Price = 39.50m, Stock = 3 },
        new Book { Isbn = "978-0547928227", Title = "The Hobbit", Price = 52.00m, Stock = 0 },
        new Book { Isbn = "978-0141439549", Title = "Middlemarch", Price = 48.75m, Stock = 5 });
    // Test kimliği customer_id=1 Ada'ya karşılık gelir
    db.Customers.AddRange(
        new Customer { Email = "ada@example.com", Name = "Ada" },
        // customer_id=2: kurumsal müşterinin satın alma sorumlusu (InstitutionBuyer)
        new Customer { Email = "nevin@example.org", Name = "Nevin" });
    await db.SaveChangesAsync();
}

using FakeProviders providers = new();
await using E2EApiFactory api = new(connectionString, providers, new E2ETokenIssuer());
api.UseKestrel(ApiPort);
api.StartServer();
Console.WriteLine($"Bookstore API listening on {api.ClientOptions.BaseAddress}");

TaskCompletionSource stopped = new();
using PosixSignalRegistration sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, Stop);
using PosixSignalRegistration sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, Stop);
await stopped.Task;
Console.WriteLine("Stopping: disposing API and SQL Server container");

void Stop(PosixSignalContext context)
{
    context.Cancel = true;
    stopped.TrySetResult();
}
