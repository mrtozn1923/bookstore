using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Bookstore.Institutional;

public static class InstitutionalRoles
{
    public const string Buyer = "InstitutionBuyer";
    public const string Approver = "InstitutionApprover";
}

// The institutional identity of the signed-in user, read only from validated token claims.
public sealed record InstitutionalCaller(string UserId, int InstitutionId, int UnitId, bool IsBuyer, bool IsApprover)
{
    public static bool TryRead(ClaimsPrincipal user, [NotNullWhen(true)] out InstitutionalCaller? caller)
    {
        caller = null;
        string? userId = user.FindFirstValue("sub");
        if (userId is null
            || !int.TryParse(user.FindFirstValue("institution_id"), out int institutionId)
            || !int.TryParse(user.FindFirstValue("unit_id"), out int unitId))
        {
            return false;
        }
        caller = new InstitutionalCaller(userId, institutionId, unitId,
            user.IsInRole(InstitutionalRoles.Buyer), user.IsInRole(InstitutionalRoles.Approver));
        return true;
    }
}
