using System.Diagnostics.CodeAnalysis;
using Bookstore.Institutional;

namespace Cd02.VirtualRoot;

// The "which unit(s)?" question at the API boundary: either the whole institution or one real unit
public sealed record InstitutionUnitScope
{
    public const int InstitutionRootId = 0;

    private InstitutionUnitScope(int? unitId) => UnitId = unitId;

    // null = the whole institution; 0 never reaches the database
    public int? UnitId { get; }
    public bool IsInstitution => UnitId is null;

    public static InstitutionUnitScope Institution { get; } = new((int?)null);

    public static InstitutionUnitScope Of(int unitId) => unitId > 0
        ? new InstitutionUnitScope(unitId)
        : throw new ArgumentOutOfRangeException(nameof(unitId), "Real units start at 1.");

    public int ToApiId() => UnitId ?? InstitutionRootId;

    // Minimal API finds this method and binds the query/route value with it
    public static bool TryParse(string? value, IFormatProvider? provider,
        [NotNullWhen(true)] out InstitutionUnitScope? scope)
    {
        scope = null;
        if (string.Equals(value, "institution", StringComparison.OrdinalIgnoreCase))
            scope = Institution;
        else if (int.TryParse(value, out int id) && id >= 0)
            scope = id == InstitutionRootId ? Institution : Of(id);
        return scope is not null;
    }

    public override string ToString() => IsInstitution ? "institution" : $"unit:{UnitId}";
}

public static class InstitutionUnitScopeQueries
{
    public static IQueryable<InstitutionUser> InScope(this IQueryable<InstitutionUser> users, InstitutionUnitScope scope)
        => scope.IsInstitution ? users : users.Where(u => u.InstitutionUnitId == scope.UnitId);
}
