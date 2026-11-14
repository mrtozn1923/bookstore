using Bookstore.Crypto.Fields;

namespace Bookstore.Customers;

public sealed class Customer
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }

    // Plaintext in memory, encrypted in the database (see BookstoreContext)
    public string Phone { get; private set; } = "";
    public string PhoneIndex { get; private set; } = "";

    public void SetPhone(string phone, BlindIndex blindIndex)
    {
        Phone = PhoneNumber.Normalize(phone);
        PhoneIndex = blindIndex.Compute(Phone);
    }
}

public static class PhoneNumber
{
    // "+90 (555) 010-00-01" and "0090 555 010 0001" -> "+905550100001"
    public static string Normalize(string phone)
    {
        string digits = string.Concat(phone.Where(char.IsAsciiDigit));
        return "+" + (phone.TrimStart().StartsWith("00", StringComparison.Ordinal) ? digits[2..] : digits);
    }
}
