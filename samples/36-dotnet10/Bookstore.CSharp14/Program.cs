var dune = new Book { Title = "  Dune  ", Stock = 3 };
Console.WriteLine($"[{dune.Title}] in stock: {dune.IsInStock}");

Book placeholder = Book.Placeholder;
Console.WriteLine($"[{placeholder.Title}] in stock: {placeholder.IsInStock}");

Book? missing = null;
missing?.Stock = 10;
Console.WriteLine(missing is null);

public class Book
{
    public string Title
    {
        get;
        set => field = value.Trim();
    } = "";

    public int Stock { get; set; }
}

public static class BookExtensions
{
    extension(Book book)
    {
        public bool IsInStock => book.Stock > 0;
    }

    extension(Book)
    {
        public static Book Placeholder => new() { Title = "Untitled book" };
    }
}
