using Bookstore.Data;
using Bookstore.Ordering;
using Bookstore.Privacy.Consent;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Tests;

public sealed class ConsentTests
{
    // Friday 13 November 2026, 16:00 UTC
    private static readonly DateTime Friday = new(2026, 11, 13, 16, 0, 0, DateTimeKind.Utc);

    private static async Task<(BookstoreContext Db, int CustomerId)> SeedAsync(TestDb testDb)
    {
        BookstoreContext db = testDb.CreateContext();
        Customer ada = FakePeople.ToCustomer(0, Friday.AddMonths(-8));
        db.Customers.Add(ada);
        await db.SaveChangesAsync();
        return (db, ada.Id);
    }

    [Fact]
    public async Task Withdrawal_wins_over_an_earlier_grant()
    {
        using var testDb = new TestDb();
        (BookstoreContext db, int id) = await SeedAsync(testDb);
        var consents = new ConsentService(db, new FixedClock(Friday));
        var messages = new CommercialMessages(db, consents, new FixedClock(Friday), new HolidayCalendar(new HashSet<DateOnly>()));

        await messages.SetEmailPermissionAsync(id, granted: true, "checkout");
        Assert.True(await messages.CanSendAsync(id, MessageKind.Commercial));

        await messages.SetEmailPermissionAsync(id, granted: false, "account-settings");
        Assert.False(await messages.CanSendAsync(id, MessageKind.Commercial));
        Assert.True(await messages.CanSendAsync(id, MessageKind.Transactional));
        Assert.Equal(2, await db.ConsentRecords.CountAsync());
    }

    [Fact]
    public async Task No_record_means_no_consent()
    {
        using var testDb = new TestDb();
        (BookstoreContext db, int id) = await SeedAsync(testDb);

        Assert.False(await new ConsentService(db, new FixedClock(Friday)).HasConsentAsync(id, ConsentCatalog.CommercialEmail));
    }

    [Fact]
    public async Task Iys_item_is_due_three_business_days_later()
    {
        using var testDb = new TestDb();
        (BookstoreContext db, int id) = await SeedAsync(testDb);
        var consents = new ConsentService(db, new FixedClock(Friday));
        var holidays = new HolidayCalendar(new HashSet<DateOnly> { new(2026, 11, 17) });
        var messages = new CommercialMessages(db, consents, new FixedClock(Friday), holidays);

        await messages.SetEmailPermissionAsync(id, granted: false, "unsubscribe-link");

        IysSyncItem item = await db.IysSyncItems.SingleAsync();
        // Monday 16, (Tuesday 17 is a holiday in this test), Wednesday 18, Thursday 19.
        Assert.Equal(new DateOnly(2026, 11, 19), item.DueDate);
    }

    [Fact]
    public async Task Choices_made_on_an_old_notice_are_refused()
    {
        using var testDb = new TestDb();
        await using BookstoreContext db = testDb.CreateContext();
        var consents = new ConsentService(db, new FixedClock(Friday));
        var decision = new CookieConsentDecision(Guid.NewGuid(), "2026-05", new() { ["analytics"] = true });

        Assert.Equal(ConsentResult.StaleNotice, await consents.RecordCookieChoicesAsync(decision, customerId: null));
        Assert.Equal(0, await db.ConsentRecords.CountAsync());
    }

    [Fact]
    public async Task Necessary_cookies_are_not_a_consent_purpose()
    {
        using var testDb = new TestDb();
        await using BookstoreContext db = testDb.CreateContext();
        var consents = new ConsentService(db, new FixedClock(Friday));
        var decision = new CookieConsentDecision(Guid.NewGuid(), ConsentCatalog.NoticeVersion, new() { ["necessary"] = false });

        Assert.Equal(ConsentResult.UnknownPurpose, await consents.RecordCookieChoicesAsync(decision, customerId: null));
    }
}
