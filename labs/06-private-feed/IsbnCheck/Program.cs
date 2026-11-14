using Bookstore.Isbn;

foreach (string isbn in args)
{
    Console.WriteLine($"{isbn}: {(Isbn13.IsValid(isbn) ? "valid" : "invalid")}");
}
