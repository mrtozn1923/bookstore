using Bogus;
using Bookstore.Data;
using Bookstore.Ordering;
using Bookstore.Privacy.Protection;

namespace Bookstore.TestData;

// Synthetic data for test and staging: no row is copied from production.
public static class SyntheticData
{
    public static List<(Customer Customer, string FakeNationalId)> Customers(int count, int seed = 2026)
    {
        Randomizer.Seed = new Random(seed);
        var faker = new Faker();
        List<(Customer, string)> result = [];
        for (int i = 0; i < count; i++)
        {
            (string first, string last, string city) = faker.PickRandom(FakePeople.All);
            var customer = new Customer
            {
                FullName = $"{first} {last}",
                Email = $"{first}.{last}.{i:000}".ToLowerInvariant() + "@example.com",
                Phone = $"+90 000 000 {i / 100 % 100:00} {i % 100:00}",
                City = city,
                AdultConfirmedAtUtc = faker.Date.Past(2, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)),
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };
            result.Add((customer, FakeNationalId(faker)));
        }
        return result;
    }

    // FAKE test value: 11 digits whose last check digit is deliberately wrong, so it can never be a real ID.
    public static string FakeNationalId(Faker faker)
    {
        string firstNine = faker.Random.Int(1, 9) + faker.Random.ReplaceNumbers("########");
        string firstTen = firstNine + NationalIdChecksum.ExpectedTenthDigit(firstNine);
        int wrongLast = (NationalIdChecksum.ExpectedEleventhDigit(firstTen) + faker.Random.Int(1, 9)) % 10;
        return firstTen + wrongLast;
    }
}
