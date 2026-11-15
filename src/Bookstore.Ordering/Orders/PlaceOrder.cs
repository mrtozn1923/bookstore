using Bookstore.Ordering.Carts;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Ordering.Orders;

public sealed record PlaceOrderRequest(string? Email, string? Isbn, int Quantity, string? CouponCode);

public sealed record ValidOrderRequest(string Email, string Isbn, int Quantity);

public sealed record CatalogBook(string Isbn, string Title, decimal Price, bool IsEbook);

public sealed record PlacedOrder(string OrderNo, string Email, CartLine Line, CheckoutTotals Totals);

// Pure validation rules: no I/O, no clock
public static class OrderValidation
{
    public const int MaxQuantity = 20;

    public static Result<string> ValidateEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? new Error("email.missing", "Email is required.")
        : email.Contains('@') && email.IndexOf('.', email.IndexOf('@')) > 0 ? email.Trim()
        : new Error("email.invalid", $"'{email}' is not a valid email address.");

    public static Result<string> ValidateIsbn(string? isbn)
    {
        string digits = new((isbn ?? "").Where(char.IsAsciiDigit).ToArray());
        if (digits.Length != 13)
            return new Error("isbn.length", "ISBN-13 must have 13 digits.");

        int sum = digits.Take(12).Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        int check = (10 - sum % 10) % 10;
        return check == digits[12] - '0' ? digits : new Error("isbn.checksum", $"{digits} has a wrong check digit.");
    }

    public static Result<int> ValidateQuantity(int quantity) =>
        quantity is >= 1 and <= MaxQuantity ? quantity
        : new Error("quantity.range", $"Quantity must be between 1 and {MaxQuantity}.");

    // Short-circuit: stops at the first error
    public static Result<ValidOrderRequest> ValidateFirstError(PlaceOrderRequest request) =>
        ValidateEmail(request.Email)
            .Bind(email => ValidateIsbn(request.Isbn)
                .Bind(isbn => ValidateQuantity(request.Quantity)
                    .Map(quantity => new ValidOrderRequest(email, isbn, quantity))));

    // Accumulating: reports every invalid field at once
    public static Result<ValidOrderRequest> Validate(PlaceOrderRequest request) =>
        Result.Combine(
            ValidateEmail(request.Email),
            ValidateIsbn(request.Isbn),
            ValidateQuantity(request.Quantity),
            (email, isbn, quantity) => new ValidOrderRequest(email, isbn, quantity));
}
