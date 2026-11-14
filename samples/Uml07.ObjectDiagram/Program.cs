using System.Globalization;
using Bookstore.Institutional.Application;
using Bookstore.Institutional.Diagrams;
using Bookstore.Institutional.Domain;
using Bookstore.Institutional.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Gerçek domain class'ları ve BookstoreContext; veritabanı bellekte (SQLite)
await using SqliteConnection connection = new("Data Source=:memory:");
await connection.OpenAsync();
await using BookstoreContext db = new(new DbContextOptionsBuilder<BookstoreContext>().UseSqlite(connection).Options);
await db.Database.EnsureCreatedAsync();
await db.Database.ExecuteSqlRawAsync("""
    INSERT INTO InstitutionUnits (Id, Name, PurchasingManagerId) VALUES (7, 'Science', NULL);
    INSERT INTO InstitutionUsers (Id, StaffNumber, FullName, Email, InstitutionUnitId, ApproverId) VALUES
        (1, 'S-0007', 'Grace Hopper', 'grace@example.com', 7, NULL),
        (3, 'S-0420', 'Alan Turing', 'alan@example.com', 7, 1),
        (4, 'S-1001', 'Ada Lovelace', 'ada@example.com', 7, 3);
    UPDATE InstitutionUnits SET PurchasingManagerId = 1 WHERE Id = 7;
    INSERT INTO BudgetCategories (Id, Code, Name, DeductsFromBudget, IsActive)
        VALUES (2, 'classroom-sets', 'Classroom Sets', 1, 1);
    INSERT INTO sqlite_sequence (name, seq) VALUES ('Orders', 41);
    """);

InstitutionUnit science = await db.InstitutionUnits.SingleAsync();
List<InstitutionUser> users = await db.InstitutionUsers.ToListAsync();
InstitutionUser grace = users.Single(u => u.Id == 1);
InstitutionUser alan = users.Single(u => u.Id == 3);
InstitutionUser ada = users.Single(u => u.Id == 4);
BudgetCategory category = await db.BudgetCategories.SingleAsync(c => c.Code == "classroom-sets");

// #42: Dune x 150, liste fiyatı 420 TL, %10 kurumsal indirim
Order order = new(ada, category, bookId: 1, quantity: 150, deliveryDate: new DateOnly(2026, 12, 21),
    note: "Science classroom sets", now: Day(2));
db.Orders.Add(order);
DiscountAgreement agreement = new(institutionId: 1, discountRate: 0.10m, new DateOnly(2025, 9, 1), endDate: null);
order.Submit(new PriceCalculator().CalculateTotal(new Money(420m, "TRY"), order.Quantity, agreement), Day(2));
order.Approve(alan, comment: null, Day(3));
order.Approve(grace, comment: "Budget checked", Day(4));
await db.SaveChangesAsync();

string diagram = new ObjectDiagram()
    .Add(order, $"order{order.Id}")
    .Add(ada, "ada").Add(category, "category")
    .Add(order.Approvals[0], "a1").Add(order.Approvals[1], "a2")
    .Add(alan, "alan").Add(grace, "grace").Add(science, "science")
    .Render();
Console.WriteLine(diagram);

static DateTimeOffset Day(int day) => new(2026, 11, day, 9, 0, 0, TimeSpan.Zero);
