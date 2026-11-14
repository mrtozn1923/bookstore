using Bookstore.Data;
using Bookstore.Data.Entities;
using Microsoft.EntityFrameworkCore;

static class Seed
{
    public static async Task ResetAsync(BookstoreContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var herbert = new Author { Name = "Frank Herbert" };
        var austen = new Author { Name = "Jane Austen" };
        var tolkien = new Author { Name = "J. R. R. Tolkien" };
        var eliot = new Author { Name = "George Eliot" };
        var kemal = new Author { Name = "Yaşar Kemal" };

        var dune = new Book { Title = "Dune", Slug = "dune", Isbn = "9780441172719", Price = 18.50m, PublishedOn = new(1965, 8, 1), Author = herbert, Tags = ["sci-fi", "classic"] };
        var emma = new Book { Title = "Emma", Isbn = "9780141439587", Price = 9.90m, PublishedOn = new(1815, 12, 23), Author = austen, Tags = ["classic"] };
        var pride = new Book { Title = "Pride and Prejudice", Isbn = "9780141439518", Price = 8.75m, PublishedOn = new(1813, 1, 28), Author = austen, Tags = ["classic", "romance"] };
        var hobbit = new Book { Title = "The Hobbit", Isbn = "9780547928227", Price = 14.00m, PublishedOn = new(1937, 9, 21), Author = tolkien, Tags = ["fantasy"] };
        var middlemarch = new Book { Title = "Middlemarch", Isbn = "9780141439549", Price = 12.75m, PublishedOn = new(1871, 12, 1), Author = eliot, Tags = ["classic"] };
        var memed = new Book { Title = "İnce Memed", Isbn = "9789750807147", Price = 11.40m, PublishedOn = new(1955, 1, 1), Author = kemal, Tags = ["classic", "turkish"] };
        db.Books.AddRange(dune, emma, pride, hobbit, middlemarch, memed);

        var ada = new Customer { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" };
        var alan = new Customer { FirstName = "Alan", LastName = "Turing", Email = "alan@example.com" };
        var grace = new Customer { FirstName = "Grace", LastName = "Hopper", Email = "grace@example.com" };
        db.Customers.AddRange(ada, alan, grace);

        var now = DateTime.UtcNow;
        Order NewOrder(Customer c, int daysAgo, params (Book Book, int Qty)[] lines) => new()
        {
            Customer = c,
            CreatedAt = now.AddDays(-daysAgo),
            Subtotal = lines.Sum(l => l.Book.Price * l.Qty),
            Items = lines.Select(l => new OrderItem { Book = l.Book, Quantity = l.Qty, UnitPrice = l.Book.Price }).ToList()
        };
        db.Orders.AddRange(
            NewOrder(ada, 2, (dune, 2), (emma, 1)),
            NewOrder(alan, 5, (hobbit, 3)),
            NewOrder(grace, 12, (dune, 1), (middlemarch, 1)),
            NewOrder(ada, 20, (memed, 2), (pride, 1)),
            NewOrder(alan, 45, (emma, 5)),
            NewOrder(grace, 60, (hobbit, 4)));
        await db.SaveChangesAsync();
    }
}
