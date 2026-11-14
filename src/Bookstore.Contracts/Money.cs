using System.Globalization;

namespace Bookstore.Contracts;

public sealed record Money(decimal Amount, string Currency)
{
    public override string ToString() => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";
}
