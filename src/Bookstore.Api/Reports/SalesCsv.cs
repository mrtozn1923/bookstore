using System.Globalization;
using System.Text;
using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;

namespace Bookstore.Api.Reports;

public static class SalesCsv
{
    // One row per sold book; no customer data goes into the sandbox.
    public static string Build(IEnumerable<Order> orders, IBookRepository books)
    {
        StringBuilder csv = new("order_id,placed_at_utc,status,isbn,title,price_try\n");
        foreach (Order order in orders.OrderBy(o => o.PlacedAtUtc))
        {
            foreach (string isbn in order.Isbns)
            {
                Book? book = books.FindByIsbn(isbn);
                csv.Append(CultureInfo.InvariantCulture,
                    $"{order.Id},{order.PlacedAtUtc:yyyy-MM-ddTHH:mm:ssZ},{order.Status},{isbn},\"{book?.Title.Replace("\"", "\"\"")}\",{book?.Price:0.00}\n");
            }
        }

        return csv.ToString();
    }
}
