namespace Bookstore.Contracts;

/// <summary>A book as returned by the Bookstore API.</summary>
/// <param name="Isbn">ISBN-13 of the book.</param>
/// <param name="Title">Title of the book.</param>
/// <param name="Author">Author name.</param>
/// <param name="ListPrice">Current list price with currency.</param>
public sealed record BookDto(string Isbn, string Title, string Author, Money ListPrice)
{
    /// <summary>Publisher name, if known.</summary>
    public string? Publisher { get; init; }
}
