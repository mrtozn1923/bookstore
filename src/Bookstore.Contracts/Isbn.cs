namespace Bookstore.Contracts;

public readonly record struct Isbn
{
    private Isbn(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse(string? input, out Isbn isbn)
    {
        isbn = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string digits = input.Replace("-", "").Replace(" ", "");
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        if ((10 - sum % 10) % 10 != digits[12] - '0')
        {
            return false;
        }

        isbn = new Isbn(digits);
        return true;
    }

    public override string ToString() => Value;
}
