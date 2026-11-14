using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Institutional.Temporal;

public sealed class PricingService(BookstoreContext db)
{
    public async Task ChangeAsync(int institutionId, DateOnly effectiveFrom, Action<InstitutionPricing> change, CancellationToken ct = default)
    {
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        InstitutionPricing current = await db.InstitutionPricing
            .SingleAsync(p => p.InstitutionId == institutionId && p.ValidFrom <= effectiveFrom && p.ValidTo > effectiveFrom, ct);

        if (current.ValidFrom == effectiveFrom)
        {
            change(current);
        }
        else
        {
            InstitutionPricing next = new()
            {
                InstitutionId = institutionId,
                PaymentTerm = current.PaymentTerm,
                DiscountRate = current.DiscountRate,
                ValidFrom = effectiveFrom,
                ValidTo = current.ValidTo
            };
            change(next);

            // close the old period first, then add the new one, so the trigger never sees a temporary overlap
            current.ValidTo = effectiveFrom;
            await db.SaveChangesAsync(ct);
            db.InstitutionPricing.Add(next);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
