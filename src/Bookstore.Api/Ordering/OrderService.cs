using Bookstore.Catalog;
using Bookstore.Data;
using Bookstore.Institutional;
using Bookstore.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Ordering;

public abstract record PlaceOrderOutcome
{
    public sealed record Placed(Order Order) : PlaceOrderOutcome;
    public sealed record CartNotFound : PlaceOrderOutcome;
    public sealed record CartEmpty : PlaceOrderOutcome;
    public sealed record OutOfStock(IReadOnlyList<string> Isbns) : PlaceOrderOutcome;
    public sealed record CartChanged : PlaceOrderOutcome;
    public sealed record UnknownInstitution(string Code) : PlaceOrderOutcome;
    public sealed record BelowInstitutionalMinimum(int Quantity) : PlaceOrderOutcome;
}

public sealed class OrderService(
    BookstoreContext db, ICatalogService catalog, IInventoryService inventory, IInstitutionService institutions, TimeProvider clock)
{
    // Kurumsal Sipariş: an order for an institution needs at least 20 books in total (Requirements Analysis series).
    public const int InstitutionalMinimumQuantity = 20;

    public async Task<PlaceOrderOutcome> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == request.CartId, ct);
        if (cart is null)
        {
            return new PlaceOrderOutcome.CartNotFound();
        }
        if (cart.Items.Count == 0)
        {
            return new PlaceOrderOutcome.CartEmpty();
        }

        InstitutionInfo? institution = null;
        if (!string.IsNullOrWhiteSpace(request.InstitutionCode))
        {
            institution = await institutions.FindByCodeAsync(request.InstitutionCode.Trim(), ct);
            if (institution is null)
            {
                return new PlaceOrderOutcome.UnknownInstitution(request.InstitutionCode);
            }
            int quantity = cart.Items.Sum(i => i.Quantity);
            if (quantity < InstitutionalMinimumQuantity)
            {
                return new PlaceOrderOutcome.BelowInstitutionalMinimum(quantity);
            }
        }

        IReadOnlyDictionary<int, BookInfo> books =
            await catalog.GetByIdsAsync([.. cart.Items.Select(i => i.BookId)], ct);

        // Stock reservation, the order and the cart removal commit or roll back together.
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);

        ReservationResult reservation = await inventory.ReserveAsync(
            [.. cart.Items.Select(i => new StockLine(i.BookId, i.Quantity))], ct);
        if (!reservation.Succeeded)
        {
            return new PlaceOrderOutcome.OutOfStock([.. reservation.UnavailableBookIds.Select(id => books[id].Isbn)]);
        }

        Customer customer = await FindOrCreateCustomerAsync(request.CustomerName, request.CustomerEmail, ct);
        Order order = new()
        {
            Customer = customer,
            InstitutionId = institution?.Id,
            PlacedAtUtc = clock.GetUtcNow().UtcDateTime,
            Status = OrderStatus.Placed
        };
        foreach (CartItem item in cart.Items)
        {
            BookInfo book = books[item.BookId];
            order.Items.Add(new OrderItem
            {
                BookId = book.Id,
                Title = book.Title,
                UnitPrice = book.Price,
                Quantity = item.Quantity,
                LineTotal = OrderItem.CalculateLineTotal(book.Price, item.Quantity)
            });
        }
        order.Total = order.Items.Sum(i => i.LineTotal);

        db.Orders.Add(order);
        db.Carts.Remove(cart);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The same cart was ordered by a parallel request in the meantime.
            return new PlaceOrderOutcome.CartChanged();
        }
        await transaction.CommitAsync(ct);

        return new PlaceOrderOutcome.Placed(order);
    }

    private async Task<Customer> FindOrCreateCustomerAsync(string name, string email, CancellationToken ct)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();
        Customer? customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == normalizedEmail, ct);
        if (customer is null)
        {
            customer = new Customer { Name = name.Trim(), Email = normalizedEmail };
            db.Customers.Add(customer);
        }
        return customer;
    }
}
