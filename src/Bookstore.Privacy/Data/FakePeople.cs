using Bookstore.Ordering;

namespace Bookstore.Data;

// All people below are fictional test data; e-mail addresses use the reserved example.com domain.
public static class FakePeople
{
    public static readonly (string First, string Last, string City)[] All =
    [
        ("Ada", "Testwell", "Ankara"),
        ("Alan", "Sampleton", "Izmir"),
        ("Grace", "Demoray", "Istanbul"),
        ("Linus", "Fakely", "Bursa"),
        ("Edsger", "Mockford", "Ankara"),
        ("Barbara", "Stubbs", "Istanbul"),
    ];

    public static Customer ToCustomer(int index, DateTime createdAtUtc)
    {
        (string first, string last, string city) = All[index % All.Length];
        return new Customer
        {
            FullName = $"{first} {last}",
            Email = $"{first}.{last}".ToLowerInvariant() + "@example.com",
            Phone = $"+90 000 000 00 {index + 1:00}",
            BirthDate = new DateOnly(1990 + index, 1 + index, 10),
            City = city,
            CreatedAtUtc = createdAtUtc,
        };
    }
}
