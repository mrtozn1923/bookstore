using Bookstore.Data;
using Bookstore.Ordering;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Privacy.Protection;

public sealed record SupportOrderView(int OrderId, string BillingName, string? NationalId);

public sealed class NationalIdVault(BookstoreContext db, PiiProtector protector)
{
    private const string Purpose = "Orders.BillingNationalId";

    public void SetBillingNationalId(Order order, string nationalId)
    {
        order.BillingNationalIdEncrypted = protector.Encrypt(nationalId, Purpose);
        order.BillingNationalIdIndex = protector.BlindIndex(nationalId, Purpose);
    }

    public Task<List<int>> FindOrderIdsAsync(string nationalId, CancellationToken ct = default)
    {
        string index = protector.BlindIndex(nationalId, Purpose);
        return db.Orders.Where(o => o.BillingNationalIdIndex == index).Select(o => o.Id).ToListAsync(ct);
    }

    // Accounting needs the full number for the invoice; store staff only see the last two digits.
    public async Task<SupportOrderView?> GetForRoleAsync(int orderId, string role, CancellationToken ct = default)
    {
        Order? order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return null;
        }

        string? nationalId = order.BillingNationalIdEncrypted is { } encrypted
            ? protector.Decrypt(encrypted, Purpose)
            : null;
        return new SupportOrderView(order.Id, order.BillingName, role == "Accounting" ? nationalId : Mask(nationalId));
    }

    public static string? Mask(string? value) =>
        value is null ? null : new string('*', value.Length - 2) + value[^2..];
}
