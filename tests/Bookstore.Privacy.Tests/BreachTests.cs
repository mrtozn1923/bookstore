using Bookstore.Data;
using Bookstore.Privacy.Breach;

namespace Bookstore.Privacy.Tests;

public sealed class BreachTests
{
    [Fact]
    public async Task Scope_lists_each_customer_once_with_categories_and_a_72_hour_deadline()
    {
        using var testDb = new TestDb();
        await using BookstoreContext db = testDb.CreateContext();
        var t = new DateTime(2026, 11, 14, 1, 0, 0, DateTimeKind.Utc);
        db.PersonalDataAccesses.AddRange(
            new PersonalDataAccess { ActorId = "staff-17", CustomerId = 3, DataCategory = "order-billing", Action = "view", AtUtc = t },
            new PersonalDataAccess { ActorId = "staff-17", CustomerId = 3, DataCategory = "customer-profile", Action = "export", AtUtc = t },
            new PersonalDataAccess { ActorId = "staff-17", CustomerId = 3, DataCategory = "customer-profile", Action = "export", AtUtc = t.AddMinutes(1) },
            new PersonalDataAccess { ActorId = "staff-02", CustomerId = 5, DataCategory = "customer-profile", Action = "view", AtUtc = t });
        await db.SaveChangesAsync();

        BreachAssessment a = await new BreachScope(db).AssessAsync("staff-17", t.AddHours(-1), t.AddHours(1), detectedAtUtc: t.AddHours(8));

        AffectedCustomer only = Assert.Single(a.Affected);
        Assert.Equal(3, only.CustomerId);
        Assert.Equal(["customer-profile", "order-billing"], only.Categories);
        Assert.Empty(a.SpecialCategories);
        Assert.Equal(t.AddHours(8 + 72), a.AuthorityDeadlineUtc);
    }
}
