using System.Globalization;
using Bookstore.Contracts;

// Sample data; the real service reads it from the Ordering module's events.
BookSummary[] bestsellers =
[
    Book("978-0-441-17271-9", "Dune", "Frank Herbert", 549.90m),
    Book("978-0-261-10357-3", "The Hobbit", "J. R. R. Tolkien", 429.00m),
    Book("978-0-14-143951-8", "Pride and Prejudice", "Jane Austen", 249.50m),
];

CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
foreach (BookSummary book in bestsellers)
{
    Console.WriteLine($"{book.Isbn}  {book.Title,-20} {book.Price.ToString("C", tr),12}");
}

Console.WriteLine($"Contracts {typeof(BookSummary).Assembly.GetName().Version} on {Environment.Version}");

static BookSummary Book(string isbn, string title, string author, decimal price) =>
    Isbn.TryParse(isbn, out Isbn parsed)
        ? new BookSummary(parsed, title, author, price)
        : throw new ArgumentException($"Invalid ISBN-13: {isbn}", nameof(isbn));
