using Bookstore.Ordering;
using Bookstore.Ordering.Institutional;

namespace Bookstore.Catalog.Tests;

// Meaningful defaults come from the builder, realistic filler (ISBN, price) from Bogus
public sealed class InstitutionalOrderBuilder
{
    private CustomerAccount customer = new("library@example.org", IsInstitutional: true, CompletedOrders: 3);
    private int copies = InstitutionalCheckout.MinimumQuantity;
    private int titles = 2;

    public InstitutionalOrderBuilder WithCopies(int total)
    {
        copies = total;
        return this;
    }

    public InstitutionalOrderBuilder AcrossTitles(int count)
    {
        titles = count;
        return this;
    }

    public InstitutionalOrderBuilder ForIndividualCustomer()
    {
        customer = customer with { IsInstitutional = false };
        return this;
    }

    public (CustomerAccount Customer, IReadOnlyList<OrderLine> Lines) Build()
    {
        List<Book> books = BookFaker.Create(seed: 20).Generate(titles);
        OrderLine[] lines =
        [
            .. books.Select((b, i) => new OrderLine(b.Isbn, copies / titles + (i < copies % titles ? 1 : 0), b.Price)),
        ];
        return (customer, lines);
    }
}
