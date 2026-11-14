namespace Bookstore.Api.Catalog;

public sealed class CatalogData
{
    private readonly List<Book> _books =
    [
        new("9780141439587", "Emma", "Jane Austen", "classics", 249.90m, 12, "penguin-tr"),
        new("9780141439518", "Pride and Prejudice", "Jane Austen", "classics", 229.90m, 0, "penguin-tr"),
        new("9780141439549", "Middlemarch", "George Eliot", "classics", 319.00m, 4, "penguin-tr"),
        new("9780141441146", "Jane Eyre", "Charlotte Bronte", "classics", 259.00m, 7, "penguin-tr"),
        new("9780141439556", "Wuthering Heights", "Emily Bronte", "classics", 239.00m, 3, "penguin-tr"),
        new("9780141439563", "Great Expectations", "Charles Dickens", "classics", 279.00m, 0, "penguin-tr"),
        new("9780141439600", "A Tale of Two Cities", "Charles Dickens", "classics", 269.00m, 5, "penguin-tr"),
        new("9780141439471", "Frankenstein", "Mary Shelley", "horror", 219.00m, 9, "penguin-tr"),
        new("9780141439846", "Dracula", "Bram Stoker", "horror", 249.00m, 6, "penguin-tr"),
        new("9780142437247", "Moby-Dick", "Herman Melville", "classics", 339.00m, 2, "penguin-tr"),
        new("9781400079988", "War and Peace", "Leo Tolstoy", "classics", 549.00m, 3, "vintage-tr"),
        new("9780143035008", "Anna Karenina", "Leo Tolstoy", "classics", 429.00m, 0, "penguin-tr"),
        new("9780143058144", "Crime and Punishment", "Fyodor Dostoevsky", "classics", 359.00m, 8, "penguin-tr"),
        new("9780140449136", "The Brothers Karamazov", "Fyodor Dostoevsky", "classics", 419.00m, 4, "penguin-tr"),
        new("9780060934347", "Don Quixote", "Miguel de Cervantes", "classics", 459.00m, 1, "harper-tr"),
        new("9780743273565", "The Great Gatsby", "F. Scott Fitzgerald", "classics", 199.00m, 15, "scribner-tr"),
        new("9780061120084", "To Kill a Mockingbird", "Harper Lee", "classics", 289.00m, 10, "harper-tr"),
        new("9780147514011", "Little Women", "Louisa May Alcott", "classics", 239.00m, 0, "penguin-tr"),
        new("9780451524935", "Nineteen Eighty-Four", "George Orwell", "dystopia", 219.00m, 20, "signet-tr"),
        new("9780060850524", "Brave New World", "Aldous Huxley", "dystopia", 249.00m, 11, "harper-tr"),
        new("9781451673319", "Fahrenheit 451", "Ray Bradbury", "dystopia", 229.00m, 0, "simon-tr"),
        new("9780441172719", "Dune", "Frank Herbert", "science-fiction", 489.00m, 14, "ace-tr"),
        new("9780553293357", "Foundation", "Isaac Asimov", "science-fiction", 299.00m, 6, "bantam-tr"),
        new("9780441569595", "Neuromancer", "William Gibson", "science-fiction", 279.00m, 2, "ace-tr"),
        new("9780261102217", "The Hobbit", "J. R. R. Tolkien", "fantasy", 349.00m, 18, "harper-tr"),
        new("9780261103252", "The Lord of the Rings", "J. R. R. Tolkien", "fantasy", 899.00m, 5, "harper-tr"),
    ];

    public IReadOnlyList<Book> All => _books;

    public Book? Find(string isbn) => _books.Find(b => b.Isbn == isbn);

    public void Add(Book book) => _books.Add(book);

    public void Replace(Book book) => _books[_books.FindIndex(b => b.Isbn == book.Isbn)] = book;

    public bool Remove(string isbn) => _books.RemoveAll(b => b.Isbn == isbn) > 0;
}
