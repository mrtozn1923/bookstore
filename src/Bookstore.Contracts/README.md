# Bookstore.Contracts

DTOs shared by the Bookstore API and its .NET clients.

```csharp
var book = new BookDto("9780441172719", "Dune", "Frank Herbert", new Money(420.00m, "TRY"));
Console.WriteLine(book.ListPrice); // 420.00 TRY
```

- Versioning: SemVer 2.0.0, see CHANGELOG.md.
- Support: the latest major gets features and fixes; the previous major gets security fixes for six months.
