namespace Bookstore.Admin.Client.Contracts;

public sealed record StockView(string Isbn, string Title, int OnHand, int Reserved, int Available);

public sealed record SetStockRequest(int QuantityOnHand);
