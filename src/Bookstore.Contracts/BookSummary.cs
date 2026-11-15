namespace Bookstore.Contracts;

/// <summary>The catalog data other services need about a book.</summary>
/// <param name="Isbn">The book's ISBN-13.</param>
/// <param name="Title">The title as printed on the cover.</param>
/// <param name="Author">The author's display name.</param>
/// <param name="Price">The current list price.</param>
/// <param name="Currency">ISO 4217 currency code; TRY unless stated otherwise.</param>
public sealed record BookSummary(Isbn Isbn, string Title, string Author, decimal Price, string Currency = "TRY");
