using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional;

public sealed record InstitutionInfo(int Id, string Code, string Name);

public interface IInstitutionService
{
    Task<InstitutionInfo?> FindByCodeAsync(string code, CancellationToken ct);
}

internal sealed class InstitutionService(BookstoreContext db) : IInstitutionService
{
    public async Task<InstitutionInfo?> FindByCodeAsync(string code, CancellationToken ct) =>
        await db.Institutions.AsNoTracking()
            .Where(i => i.Code == code)
            .Select(i => new InstitutionInfo(i.Id, i.Code, i.Name))
            .FirstOrDefaultAsync(ct);
}
