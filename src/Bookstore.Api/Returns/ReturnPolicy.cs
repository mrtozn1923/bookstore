using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;

namespace Bookstore.Api.Returns;

// A draft that matches the schema can still break the business rules; these checks run in code.
public sealed class ReturnPolicy(IOrderRepository orders, IBookRepository books)
{
    public IReadOnlyList<string> Check(ReturnRequestDraft draft, CurrentCustomer customer)
    {
        List<string> problems = [];

        if (draft.OrderId is null)
        {
            problems.Add("The order number is missing; ask the customer for it.");
            return problems;
        }

        Order? order = orders.Find(draft.OrderId);
        if (order is null || order.CustomerId != customer.Id)
        {
            problems.Add($"Order {draft.OrderId} was not found for this customer.");
            return problems;
        }

        if (order.Status != "Shipped" && order.Status != "Delivered")
        {
            problems.Add($"Order {order.Id} is '{order.Status}'; it can be cancelled, not returned.");
        }

        if (draft.Items.Count == 0)
        {
            problems.Add("No items were named.");
        }

        HashSet<string> titlesInOrder = order.Isbns
            .Select(books.FindByIsbn)
            .Where(b => b is not null)
            .Select(b => b!.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (ReturnItem item in draft.Items)
        {
            if (!titlesInOrder.Contains(item.Title))
            {
                problems.Add($"'{item.Title}' is not in order {order.Id}.");
            }

            if (item.Quantity < 1)
            {
                problems.Add($"'{item.Title}' has an invalid quantity ({item.Quantity}).");
            }
        }

        return problems;
    }
}
