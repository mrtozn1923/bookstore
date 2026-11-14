using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Bookstore.Api.Catalog;
using Bookstore.Api.Common;
using Bookstore.Api.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Ordering;

public sealed record CartItemResponse(string Isbn, string Title, int Quantity, Money UnitPrice);

public sealed record CartResponse(string CustomerId, IReadOnlyList<CartItemResponse> Items, Money Total);

public sealed record SetCartItemRequest([property: Range(1, 99, ErrorMessage = "out-of-range")] int Quantity);

public sealed class CartData
{
    // customerId -> (isbn -> quantity)
    private readonly Dictionary<string, Dictionary<string, int>> _carts = new()
    {
        ["ada"] = new() { ["9780441172719"] = 1, ["9780141439587"] = 2 },
    };

    public Dictionary<string, int> For(string customerId) =>
        _carts.TryGetValue(customerId, out Dictionary<string, int>? cart) ? cart : _carts[customerId] = [];
}

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        // The cart is a singleton sub-resource of a customer; "me" is an alias for the caller.
        RouteGroupBuilder cart = app.MapGroup("/customers/{customerId}/cart").RequireAuthorization();
        cart.MapGet("", GetCart).WithName("getCart");
        cart.MapPut("/items/{isbn}", SetCartItem).WithName("setCartItem");
        cart.MapDelete("/items/{isbn}", RemoveCartItem).WithName("removeCartItem");
        return app;
    }

    private static Results<Ok<CartResponse>, ProblemHttpResult> GetCart(
        string customerId, ClaimsPrincipal principal, CartData carts, CatalogData catalog)
    {
        if (ResolveCustomer(customerId, principal) is not { } id)
        {
            return CustomerNotFound(customerId);
        }

        return TypedResults.Ok(ToResponse(id, carts.For(id), catalog));
    }

    // PUT creates or replaces one line; sending the same quantity twice leaves the same cart (idempotent).
    private static Results<Ok<CartItemResponse>, Created<CartItemResponse>, ProblemHttpResult> SetCartItem(
        string customerId, string isbn, SetCartItemRequest request, ClaimsPrincipal principal, CartData carts,
        CatalogData catalog)
    {
        if (ResolveCustomer(customerId, principal) is not { } id)
        {
            return CustomerNotFound(customerId);
        }

        if (catalog.Find(isbn) is not { } book)
        {
            return ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/books/{isbn}"]);
        }

        if (request.Quantity > book.StockQuantity)
        {
            return ProblemCatalog.Create(ProblemCatalog.InsufficientStock, [book.StockQuantity, book.Title],
                new Dictionary<string, object?> { ["isbn"] = isbn, ["availableQuantity"] = book.StockQuantity });
        }

        Dictionary<string, int> cart = carts.For(id);
        bool existed = cart.ContainsKey(isbn);
        cart[isbn] = request.Quantity;
        CartItemResponse item = new(book.Isbn, book.Title, request.Quantity, new Money(book.Price, "TRY"));
        return existed
            ? TypedResults.Ok(item)
            : TypedResults.Created($"/customers/{customerId}/cart/items/{isbn}", item);
    }

    private static Results<NoContent, ProblemHttpResult> RemoveCartItem(
        string customerId, string isbn, ClaimsPrincipal principal, CartData carts)
    {
        if (ResolveCustomer(customerId, principal) is not { } id)
        {
            return CustomerNotFound(customerId);
        }

        return carts.For(id).Remove(isbn)
            ? TypedResults.NoContent()
            : ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/customers/{customerId}/cart/items/{isbn}"]);
    }

    internal static string? ResolveCustomer(string customerId, ClaimsPrincipal principal)
    {
        DemoUser user = principal.Current();
        string id = customerId == "me" ? user.Name : customerId;
        return user.Role == "Customer" && id == user.Name ? id : null;
    }

    internal static CartResponse ToResponse(string customerId, Dictionary<string, int> cart, CatalogData catalog)
    {
        List<CartItemResponse> items =
        [
            .. cart.Select(line => (Book: catalog.Find(line.Key)!, Quantity: line.Value))
                .Select(x => new CartItemResponse(x.Book.Isbn, x.Book.Title, x.Quantity, new Money(x.Book.Price, "TRY"))),
        ];
        return new CartResponse(customerId, items, new Money(items.Sum(i => i.Quantity * i.UnitPrice.Amount), "TRY"));
    }

    internal static ProblemHttpResult CustomerNotFound(string customerId) =>
        ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/customers/{customerId}"]);
}
