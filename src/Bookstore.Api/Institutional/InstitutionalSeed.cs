namespace Bookstore.Institutional;

// Two sample institutions with a small unit hierarchy.
internal static class InstitutionalSeed
{
    public static readonly Institution[] Institutions =
    [
        new() { Id = 1, Name = "Riverside High School", Code = "RHS", Kind = InstitutionKind.School },
        new() { Id = 2, Name = "Central City Library", Code = "CCL", Kind = InstitutionKind.Library }
    ];

    public static readonly InstitutionUnit[] Units =
    [
        new() { Id = 1, InstitutionId = 1, Name = "North Campus" },
        new() { Id = 2, InstitutionId = 1, Name = "South Campus" },
        new() { Id = 3, InstitutionId = 1, ParentUnitId = 1, Name = "Science Department" },
        new() { Id = 4, InstitutionId = 2, Name = "Main Branch" },
        new() { Id = 5, InstitutionId = 2, ParentUnitId = 4, Name = "Children's Section" }
    ];
}
