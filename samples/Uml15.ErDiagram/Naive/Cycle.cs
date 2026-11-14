using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.ErDiagram;

public static class Cycle
{
    public static async Task NaiveAsync(string connectionString)
    {
        await using Naive.NaiveContext db = new(connectionString);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        // Birim ve satın alma müdürü aynı SaveChanges'ta
        Naive.InstitutionUnit science = new() { Name = "Science" };
        Naive.InstitutionUser grace = new()
        {
            StaffNumber = "S-0007", FullName = "Grace Hopper", Email = "grace@example.com",
            InstitutionUnit = science
        };
        science.PurchasingManager = grace;
        db.AddRange(science, grace);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static async Task TwoStepAsync(string connectionString)
    {
        await using Fixed.FixedContext db = new(connectionString);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        await using var tx = await db.Database.BeginTransactionAsync();
        // 1) Birim satın alma müdürü olmadan (PurchasingManagerId NULL) eklenir
        var science = new Fixed.InstitutionUnit { Name = "Science" };
        db.Add(science);
        await db.SaveChangesAsync();

        // 2) Kullanıcı eklenir ve birimin satın alma müdürü olarak atanır
        var grace = new Fixed.InstitutionUser
        {
            StaffNumber = "S-0007", FullName = "Grace Hopper", Email = "grace@example.com",
            InstitutionUnit = science
        };
        science.PurchasingManager = grace;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        Console.WriteLine($"Unit {science.Id}: purchasing manager {science.PurchasingManagerId}; user {grace.Id}: unit {grace.InstitutionUnitId}");
    }
}
