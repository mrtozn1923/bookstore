using Bookstore.Institutional.Contracts;

namespace Bookstore.Institutional;

internal interface IBudgetService
{
    bool HasEnoughBudget(int unitId, decimal amount);
}

internal sealed class BudgetService : IBudgetService
{
    // Science biriminin 2026 sınıf seti bütçesi: 120.000 - 45.000
    public bool HasEnoughBudget(int unitId, decimal amount) => amount <= 75_000m;
}

internal sealed class ApprovedOrderQuery : IApprovedOrderQuery
{
    private static readonly (int OrderId, int InstitutionId, decimal Total, DateOnly DeliveryDate)[] Approved =
    [
        (43, 1, 6_840.00m, new DateOnly(2026, 11, 20)),
        (42, 1, 56_700.00m, new DateOnly(2026, 12, 21)),
    ];

    // Teslim tarihi verilen aya düşen onaylı siparişlerin toplamı
    public Task<decimal> GetApprovedTotalAsync(int institutionId, int year, int month, CancellationToken ct = default) =>
        Task.FromResult(Approved
            .Where(o => o.InstitutionId == institutionId && o.DeliveryDate.Year == year && o.DeliveryDate.Month == month)
            .Sum(o => o.Total));
}
