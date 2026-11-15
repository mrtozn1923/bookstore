using System.Diagnostics.CodeAnalysis;

namespace Bookstore.Bcl.Text;

// An ISBN-13 that knows how to print itself: "G" -> 9780441172719, "P" -> ISBN 9780441172719.
public readonly struct Isbn : ISpanFormattable, IEquatable<Isbn>
{
    private readonly string _digits;

    private Isbn(string digits) => _digits = digits;

    public static Isbn Parse(string input) =>
        TryParse(input, out Isbn isbn) ? isbn : throw new FormatException($"'{input}' is not a valid ISBN-13.");

    public static bool TryParse([NotNullWhen(true)] string? input, out Isbn isbn)
    {
        bool ok = IsbnNormalizer.TryNormalize(input, out string? digits);
        isbn = ok ? new Isbn(digits!) : default;
        return ok;
    }

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider) => format switch
    {
        null or "" or "G" => _digits ?? "",
        "P" => "ISBN " + _digits,
        _ => throw new FormatException($"The '{format}' format is not supported. Use G or P."),
    };

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        ReadOnlySpan<char> prefix = format switch
        {
            "" or "G" => "",
            "P" => "ISBN ",
            _ => throw new FormatException($"The '{format}' format is not supported. Use G or P."),
        };

        string digits = _digits ?? "";
        if (destination.Length < prefix.Length + digits.Length)
        {
            charsWritten = 0;
            return false;
        }

        prefix.CopyTo(destination);
        digits.CopyTo(destination[prefix.Length..]);
        charsWritten = prefix.Length + digits.Length;
        return true;
    }

    public bool Equals(Isbn other) => string.Equals(_digits, other._digits, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is Isbn other && Equals(other);
    public override int GetHashCode() => _digits is null ? 0 : StringComparer.Ordinal.GetHashCode(_digits);
    public static bool operator ==(Isbn left, Isbn right) => left.Equals(right);
    public static bool operator !=(Isbn left, Isbn right) => !left.Equals(right);
}
