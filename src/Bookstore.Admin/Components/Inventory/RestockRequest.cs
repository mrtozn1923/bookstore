namespace Bookstore.Admin.Components.Inventory;

// What BookRow tells its parent when the user restocks a book.
public sealed record RestockRequest(string Isbn, int Amount);
