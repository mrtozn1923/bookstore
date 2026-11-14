using System.Globalization;

namespace Bookstore.Contracts;

/// <summary>An amount of money in a given currency.</summary>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">ISO 4217 currency code, for example TRY.</param>
public sealed record Money(decimal Amount, string Currency)
{
    /// <inheritdoc />
    public override string ToString() => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency.ToUpperInvariant()}";
}
