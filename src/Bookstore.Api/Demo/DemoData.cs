using Bookstore.Institutional.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Demo;

// Yalnız yerel deneme için: veritabanını sıfırdan kurar ve serinin örnek verisini yükler
public static class DemoData
{
    public static async Task ResetAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        // Birim önce satın alma müdürü olmadan eklenir, müdür kullanıcı eklendikten sonra atanır (ER yazısı)
        await db.Database.ExecuteSqlRawAsync("""
            SET IDENTITY_INSERT InstitutionUnits ON;
            INSERT INTO InstitutionUnits (Id, Name, PurchasingManagerId) VALUES (7, N'Science', NULL);
            SET IDENTITY_INSERT InstitutionUnits OFF;
            SET IDENTITY_INSERT InstitutionUsers ON;
            INSERT INTO InstitutionUsers (Id, StaffNumber, FullName, Email, InstitutionUnitId, ApproverId) VALUES
                (1, N'S-0007', N'Grace Hopper', N'grace@example.com', 7, NULL),
                (2, N'S-0300', N'Margaret Hamilton', N'margaret@example.com', 7, NULL),
                (3, N'S-0420', N'Alan Turing', N'alan@example.com', 7, 1),
                (4, N'S-1001', N'Ada Lovelace', N'ada@example.com', 7, 3);
            SET IDENTITY_INSERT InstitutionUsers OFF;
            UPDATE InstitutionUnits SET PurchasingManagerId = 1 WHERE Id = 7;
            INSERT INTO BudgetCategories (Code, Name, DeductsFromBudget, IsActive) VALUES
                (N'library-books', N'Library Books', 1, 1), (N'classroom-sets', N'Classroom Sets', 1, 1),
                (N'teacher-resources', N'Teacher Resources', 1, 1), (N'donation', N'Donation', 0, 1);
            INSERT INTO UnitBudgets (InstitutionUnitId, BudgetCategoryId, Year, AllocatedAmount, SpentAmount)
                VALUES (7, 2, 2026, 120000, 45000);
            INSERT INTO DiscountAgreements (InstitutionId, DiscountRate, StartDate, EndDate)
                VALUES (1, 0.10, '2025-09-01', NULL);
            """);
    }
}
