using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Attachments;

public sealed class AttachmentAccess(BookstoreContext db)
{
    // an attachment's authorization isn't on its own row, it's on its owner
    public async Task<bool> CanReadAsync(int attachmentId, UserInfo user, CancellationToken ct = default)
    {
        Attachment? a = await db.Attachments
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.Invoice)
            .Include(x => x.InstitutionUser)
            .SingleOrDefaultAsync(x => x.Id == attachmentId, ct);

        return a switch
        {
            null => false,
            { Order: Order o } => o.InstitutionId == user.InstitutionId
                && (o.PlacedById == user.Id || user.Roles.Contains("InstitutionApprover")),
            { Invoice: Invoice inv } => inv.InstitutionId == user.InstitutionId
                && (user.Roles.Contains("Accounting") || user.Roles.Contains("InstitutionApprover")),
            { InstitutionUser: InstitutionUser iu } => iu.InstitutionId == user.InstitutionId
                && (iu.Id == user.Id || user.Roles.Contains("PortalAdmin")),
            _ => false
        };
    }
}
