using Bookstore.Institutional.ErDiagram;
using Bookstore.Institutional.Infrastructure;
using Bookstore.Tools;
using Microsoft.EntityFrameworkCore;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
string mode = args.FirstOrDefault() ?? "er";

switch (mode)
{
    // Gerçek modelden Mermaid erDiagram (veritabanına bağlanmaz)
    case "er":
    {
        DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer().Options;
        using BookstoreContext db = new(options);
        Console.Write(MermaidErWriter.Write(db.Model));
        break;
    }
    // Varyant modelin DDL'i ve ER'si: ayrı tablodaki ek dosya, computed column
    case "script":
    case "er-variant":
    {
        DbContextOptions<ErVariantContext> options = new DbContextOptionsBuilder<ErVariantContext>().UseSqlServer().Options;
        using ErVariantContext db = new(options);
        Console.Write(mode == "script" ? db.Database.GenerateCreateScript() : MermaidErWriter.Write(db.Model));
        break;
    }
    case "tph":
    {
        using TphContext db = new();
        Console.Write(MermaidErWriter.Write(db.Model));
        break;
    }
    case "tpt":
    {
        using TptContext db = new();
        Console.Write(MermaidErWriter.Write(db.Model));
        break;
    }
    case "cycle":
        await Cycle.NaiveAsync(connectionString);
        break;
    case "two-step":
        await Cycle.TwoStepAsync(connectionString);
        break;
}
