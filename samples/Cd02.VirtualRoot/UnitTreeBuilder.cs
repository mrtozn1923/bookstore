namespace Cd02.VirtualRoot;

public sealed record UnitRow(int Id, int? ParentId, string Name);

public sealed record UnitNode(int Id, string Name, string Kind, IReadOnlyList<UnitNode> Children);

public static class UnitTreeBuilder
{
    public static UnitNode Build(string institutionName, IReadOnlyList<UnitRow> rows)
    {
        ILookup<int?, UnitRow> byParent = rows.ToLookup(r => r.ParentId);
        int visited = 0;

        UnitNode Map(UnitRow row)
        {
            visited++;
            List<UnitNode> children = byParent[row.Id].OrderBy(c => c.Name).Select(Map).ToList();
            return new UnitNode(row.Id, row.Name, "unit", children);
        }

        // The root is not in the database: units whose ParentId is null become its children
        List<UnitNode> topLevel = byParent[null].OrderBy(r => r.Name).Select(Map).ToList();

        // Rows in a cycle or with a lost parent are unreachable from the root; do not drop them silently
        if (visited != rows.Count)
            throw new InvalidOperationException(
                $"Unit tree is broken: {rows.Count - visited} of {rows.Count} rows are unreachable from the root.");

        return new UnitNode(InstitutionUnitScope.InstitutionRootId, institutionName, "institution", topLevel);
    }
}
