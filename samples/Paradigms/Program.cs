using System.Globalization;
using Bookstore.Catalog;
using Bookstore.Paradigms;

decimal imperative = StockValue.Imperative(SampleBooks.All, "Jane Austen");
decimal declarative = StockValue.Declarative(SampleBooks.All, "Jane Austen");
decimal matched = StockValue.Matched(SampleBooks.All, "Jane Austen");
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{imperative} {declarative} {matched}"));

// .NET generics are kept at run time (reified); TypeScript and Java erase them
Console.WriteLine(new List<Book>().GetType());
