using System.Globalization;
using System.Text;

namespace Bookstore.Ordering;

public static class Receipt
{
    public static string Render(Order order)
    {
        StringBuilder text = new();
        text.AppendLine($"Order {order.OrderNumber}");
        foreach (OrderLine line in order.Lines)
        {
            text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{line.Isbn}  x{line.Quantity}  {line.UnitPrice * line.Quantity:0.00}"));
        }

        text.Append(string.Create(CultureInfo.InvariantCulture, $"Total  {order.Total:0.00}"));
        return text.ToString();
    }

    public static string FormatTotal(Order order, string cultureName) =>
        order.Total.ToString("C", CultureInfo.GetCultureInfo(cultureName));
}
