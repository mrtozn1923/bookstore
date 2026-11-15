using Bookstore.Ordering.Carts;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Ordering.Orders;

public sealed record Reservation(string Isbn, int Quantity);

public interface IStockReservations
{
    Task<Result<Reservation>> ReserveAsync(string isbn, int quantity, CancellationToken ct);
}

// Shell around the pure rules: reads the clock, reserves stock, writes the log
public sealed class OrderPlacement(
    IReadOnlyDictionary<string, CatalogBook> catalog,
    IStockReservations stock,
    Campaign campaign,
    TimeProvider clock,
    Func<string> nextOrderNo,
    TextWriter log)
{
    public Result<CatalogBook> FindBook(string isbn) =>
        catalog.Find(isbn).Match(
            book => Result<CatalogBook>.Success(book),
            () => new Error("book.not_found", $"No book with ISBN {isbn}."));

    public Task<Result<PlacedOrder>> PlaceAsync(PlaceOrderRequest request, CancellationToken ct = default) =>
        OrderValidation.Validate(request)
            .Bind(valid => FindBook(valid.Isbn).Map(book => (valid, book)))
            .BindAsync(draft => stock.ReserveAsync(draft.book.Isbn, draft.valid.Quantity, ct).MapAsync(_ => draft))
            .MapAsync(draft => Price(draft.valid, draft.book, request.CouponCode))
            .TapAsync(order => log.WriteLine($"[order] {order.OrderNo} {order.Line.Quantity} x {order.Line.Isbn} total={order.Totals.Total}"));

    // The same flow with await and early returns
    public async Task<Result<PlacedOrder>> PlaceWithAwaitAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        Result<ValidOrderRequest> valid = OrderValidation.Validate(request);
        if (!valid.IsSuccess)
            return Result<PlacedOrder>.Failure(valid.Errors);

        ValidOrderRequest input = valid.Match(v => v, _ => throw new InvalidOperationException());
        Result<CatalogBook> book = FindBook(input.Isbn);
        if (!book.IsSuccess)
            return Result<PlacedOrder>.Failure(book.Errors);

        CatalogBook found = book.Match(b => b, _ => throw new InvalidOperationException());
        Result<Reservation> reservation = await stock.ReserveAsync(found.Isbn, input.Quantity, ct);
        if (!reservation.IsSuccess)
            return Result<PlacedOrder>.Failure(reservation.Errors);

        PlacedOrder order = Price(input, found, request.CouponCode);
        log.WriteLine($"[order] {order.OrderNo} {order.Line.Quantity} x {order.Line.Isbn} total={order.Totals.Total}");
        return order;
    }

    private PlacedOrder Price(ValidOrderRequest valid, CatalogBook book, string? couponCode)
    {
        CartLine line = new(book.Isbn, book.Title, book.Price, valid.Quantity, book.IsEbook);
        Cart cart = Cart.Empty.Add(line) with { CouponCode = CouponLookup.NormalizeCode(couponCode).GetValueOrDefault("") };
        CheckoutTotals totals = CheckoutCalculator.Calculate(cart, isMember: false, campaign, clock.GetUtcNow());
        return new PlacedOrder(nextOrderNo(), valid.Email, line, totals);
    }
}

public sealed class InMemoryStock(Dictionary<string, int> available) : IStockReservations
{
    public Task<Result<Reservation>> ReserveAsync(string isbn, int quantity, CancellationToken ct)
    {
        int left = available.GetValueOrDefault(isbn);
        if (left < quantity)
            return Task.FromResult(Result<Reservation>.Failure(new Error("stock.insufficient", $"Only {left} left for {isbn}.")));

        available[isbn] = left - quantity;
        return Task.FromResult(Result<Reservation>.Success(new Reservation(isbn, quantity)));
    }
}
