using System.Text.RegularExpressions;

namespace Bookstore.Prompts.Tasks.ReturnCheck;

// The model may only copy an order number, never compose one
public static partial class ReturnCheckGuard
{
    public static ReturnCheckResult Apply(ReturnCheckResult result, string message)
    {
        if (result.OrderNumber is null)
        {
            return result;
        }
        string digits = result.OrderNumber.Trim().TrimStart('#');
        bool copied = OrderNumberPattern().Matches(message).Any(m => m.Value == digits);
        return copied ? result with { OrderNumber = digits } : result with { OrderNumber = null };
    }

    [GeneratedRegex(@"(?<!\d|\d[.,])\d{5}(?!\d|[.,]\d)")]
    private static partial Regex OrderNumberPattern();
}
