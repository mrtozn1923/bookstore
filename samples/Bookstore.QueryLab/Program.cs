using System.Globalization;
using Bookstore.Data;
using Bookstore.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Bookstore")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
var section = args.FirstOrDefault() ?? "strings";

var options = new DbContextOptionsBuilder<BookstoreContext>()
    .UseSqlServer(connectionString)
    .Options;
await using var db = new BookstoreContext(options);

void Show<T>(IQueryable<T> query)
{
    Console.WriteLine(query.ToQueryString());
    foreach (var row in query.ToList())
    {
        Console.WriteLine($"  {row}");
    }
    Console.WriteLine();
}

switch (section)
{
    case "setup":
        await Seed.ResetAsync(db);
        Console.WriteLine("Database ready.");
        break;

    case "strings":
        var term = "dune";
        Show(db.Books.Where(b => b.Title.Contains(term)).Select(b => b.Title));
        Show(db.Books.Where(b => b.Title.StartsWith("The")).Select(b => b.Title));
        Show(db.Books.Where(b => b.Title.StartsWith(term)).Select(b => b.Title));
        var userInput = "50%";
        Show(db.Books.Where(b => b.Title.Contains(userInput)).Select(b => b.Title));
        Show(db.Books.Where(b => b.Title.ToLower().StartsWith("dune")).Select(b => b.Title));
        Show(db.Books.Where(b => b.Title.ToLower().Contains(term.ToLower())).Select(b => b.Title));
        Show(db.Books.Where(b => b.Isbn.EndsWith("_9")).Select(b => b.Title));
        Show(db.Books.OrderBy(b => b.Id).Take(2).Select(b => new
        {
            b.Title,
            Upper = b.Title.ToUpper(),
            First3 = b.Title.Substring(0, 3),
            Space = b.Title.IndexOf(" "),
            b.Title.Length,
            Trimmed = b.Title.Trim()
        }));
        Show(db.Authors.OrderBy(a => a.Name).Select(a => new
        {
            a.Name,
            Titles = string.Join(", ", a.Books.OrderBy(b => b.Title).Select(b => b.Title))
        }));
        Show(db.Books
            .GroupBy(b => b.Author.Name)
            .OrderBy(g => g.Key)
            .Select(g => new { Author = g.Key, Titles = string.Join(", ", g.OrderBy(b => b.Title).Select(b => b.Title)) }));
        break;

    case "collation":
        Show(db.Books.Where(b => b.Title.StartsWith("ince")).Select(b => b.Title));
        Show(db.Books.Where(b => EF.Functions.Collate(b.Title, "Turkish_CI_AS").StartsWith("ince")).Select(b => b.Title));
        break;

    case "dates":
        var now = DateTime.UtcNow;
        Show(db.Orders.OrderBy(o => o.Id).Take(2).Select(o => new
        {
            o.Id,
            Day = o.CreatedAt.Date,
            o.CreatedAt.Year,
            o.CreatedAt.Month,
            DueDate = o.CreatedAt.AddDays(14),
            Age = EF.Functions.DateDiffDay(o.CreatedAt, now)
        }));
        Show(db.Books.Where(b => b.PublishedOn.Year < 1900).Select(b => new { b.Title, Centenary = b.PublishedOn.AddYears(100) }));
        break;

    case "bestsellers":
        var since = DateTime.UtcNow.AddDays(-30);
        Show(db.OrderItems
            .Where(i => i.Order.CreatedAt >= since)
            .GroupBy(i => i.Book.Title)
            .Select(g => new { Title = g.Key, Units = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Units)
            .Take(3));
        Show(db.OrderItems
            .Where(i => EF.Functions.DateDiffDay(i.Order.CreatedAt, DateTime.UtcNow) <= 30)
            .GroupBy(i => i.Book.Title)
            .Select(g => new { Title = g.Key, Units = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Units)
            .Take(3));
        break;

    case "math":
        Show(db.Books.Where(b => b.Price > 12).Select(b => new
        {
            b.Title,
            Discounted = Math.Round(b.Price * 0.85m, 2),
            Floor = Math.Floor(b.Price),
            Diff = Math.Abs(b.Price - 12.50m)
        }));
        Show(db.Books.OrderBy(b => Guid.NewGuid()).Select(b => b.Title).Take(1));
        break;

    case "collections":
        int[] ids = [1, 3, 5];
        Show(db.Books.Where(b => ids.Contains(b.Id)).Select(b => b.Title));
        Show(db.Books.Where(b => b.Tags.Contains("classic")).Select(b => b.Title));
        Show(db.Books.Where(b => b.Tags.Count > 1).Select(b => new { b.Title, TagCount = b.Tags.Count }));
        break;

    case "functions":
        Show(db.Books.Where(b => EF.Functions.Like(b.Title, "%[Mm]arch")).Select(b => b.Title));
        Show(db.Books.Where(b => EF.Functions.Like(b.Isbn, "978!_%", "!")).Select(b => b.Title));
        Show(db.Books.OrderBy(b => b.Id).Take(2).Select(b => new { b.Title, Bytes = EF.Functions.DataLength(b.Title) }));
        Console.WriteLine(db.Books.Where(b => EF.Functions.FreeText(b.Title, "hobbit")).Select(b => b.Title).ToQueryString());
        Console.WriteLine();
        try
        {
            db.Books.Where(b => EF.Functions.FreeText(b.Title, "hobbit")).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
        break;

    case "udf":
        Show(db.Books
            .Where(b => BookstoreFunctions.DiscountedPrice(b.Price, 20) < 10)
            .Select(b => new { b.Title, b.Price, Sale = BookstoreFunctions.DiscountedPrice(b.Price, 20) }));
        var from = DateTime.UtcNow.AddDays(-30);
        Show(db.BestSellers(from)
            .Join(db.Books, s => s.BookId, b => b.Id, (s, b) => new { b.Title, s.Units })
            .OrderByDescending(x => x.Units));
        break;

    case "fluent":
        await using (var logDb = new BookstoreContext(new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlServer(connectionString)
            .LogTo(Console.WriteLine, [RelationalEventId.CommandExecuted])
            .Options))
        {
            logDb.Authors.Add(new Author { Name = "Ursula K. Le Guin" });
            await logDb.SaveChangesAsync();

            var customer = await logDb.Customers.FirstAsync();
            var order = new Order { Customer = customer, Subtotal = 40m, ShippingFee = 0m };
            logDb.Orders.Add(order);
            await logDb.SaveChangesAsync();
            Console.WriteLine($"OrderNumber={order.OrderNumber} ShippingFee={order.ShippingFee} TotalAmount={order.TotalAmount}");
        }
        break;

    case "collation31":
        Show(db.Books.Where(b => b.Title.StartsWith("ince")).Select(b => b.Title));
        Show(db.BookSalesSummaries.Where(s => s.Units > 2).OrderByDescending(s => s.Units).Select(s => new { s.Title, s.Units }));
        break;

    case "constraints":
        foreach (var book in new[]
        {
            new Book { Title = "Dune Messiah", Slug = "dune", Isbn = "9780593098233", Price = 17m, AuthorId = 1 },
            new Book { Title = "Children of Dune", Isbn = "9780593098240", Price = -1m, AuthorId = 1 }
        })
        {
            db.ChangeTracker.Clear();
            db.Books.Add(book);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
            }
        }
        break;

    case "untranslatable":
        var tr = new CultureInfo("tr-TR");
        try
        {
            db.Books.Where(b => b.Title.ToUpper(tr) == "İNCE MEMED").ToList();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
        Show(db.Books.Where(b => b.Price > 12).Select(b => b.Title.ToUpper(tr)));
        break;
}
