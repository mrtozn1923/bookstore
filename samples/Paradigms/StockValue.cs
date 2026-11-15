using Bookstore.Catalog;

namespace Bookstore.Paradigms;

// The same question in three styles: what is the stock of an author's books worth?
public static class StockValue
{
    // imperative: say how, step by step, by changing a variable
    public static decimal Imperative(IEnumerable<Book> books, string author)
    {
        decimal total = 0;
        foreach (Book book in books)
        {
            if (book.Author == author)
                total += book.Price * book.Stock;
        }
        return total;
    }

    // declarative and functional: say what, with LINQ and lambdas
    public static decimal Declarative(IEnumerable<Book> books, string author) =>
        books.Where(b => b.Author == author).Sum(b => b.Price * b.Stock);

    // pattern matching over immutable records
    public static decimal Matched(IEnumerable<Book> books, string author) =>
        books.Sum(book => book switch
        {
            { Stock: > 0 } when book.Author == author => book.Price * book.Stock,
            _ => 0m,
        });
}
