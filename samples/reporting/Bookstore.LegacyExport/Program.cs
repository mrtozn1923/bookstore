using Bookstore.Contracts;

// Still on .NET 8: exports series titles in shelf order for the old accounting system.
string[] titles = ["Volume 10", "The Hobbit", "Volume 2", "volume 1"];

foreach (string title in titles.Order(BookTitleComparer.Instance))
{
    Console.WriteLine(title);
}

Console.WriteLine($"Contracts {typeof(BookTitleComparer).Assembly.GetName().Version} on {Environment.Version}");
