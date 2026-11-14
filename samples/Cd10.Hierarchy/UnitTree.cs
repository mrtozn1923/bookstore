using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Institutional.Hierarchy;

public sealed class UnitNode(int id, int? parentId, string name)
{
    public int Id { get; } = id;
    public int? ParentId { get; } = parentId;
    public string Name { get; } = name;
    public List<UnitNode> Children { get; } = [];
}

public sealed record UnitChildDto(int Id, string Name, bool HasChildren);

public enum MoveResult { Moved, NotFound, WouldCreateCycle }

public static class UnitTree
{
    public static IReadOnlyList<UnitNode> Build(IEnumerable<UnitNode> flat)
    {
        // O(n): every node enters the dictionary once and is attached to its parent once
        Dictionary<int, UnitNode> byId = flat.ToDictionary(n => n.Id);
        List<UnitNode> roots = [];
        foreach (UnitNode node in byId.Values)
        {
            if (node.ParentId is int parentId && byId.TryGetValue(parentId, out UnitNode? parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }
        return roots;
    }
}

public sealed class UnitMoveService(BookstoreContext db)
{
    public async Task<MoveResult> MoveAsync(int unitId, int newParentId, CancellationToken ct = default)
    {
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        InstitutionUnit? node = await db.InstitutionUnits.SingleOrDefaultAsync(u => u.Id == unitId, ct);
        InstitutionUnit? newParent = await db.InstitutionUnits.SingleOrDefaultAsync(u => u.Id == newParentId, ct);
        if (node is null || newParent is null)
        {
            return MoveResult.NotFound;
        }
        // a cycle appears if the new parent is the moved node itself or below it
        if (newParent.Node.IsDescendantOf(node.Node))
        {
            return MoveResult.WouldCreateCycle;
        }

        HierarchyId oldNode = node.Node;
        HierarchyId? lastChild = await db.InstitutionUnits
            .Where(u => u.Node.GetAncestor(1) == newParent.Node)
            .MaxAsync(u => (HierarchyId?)u.Node, ct);
        HierarchyId newNode = newParent.Node.GetDescendant(lastChild, null);

        // the whole subtree moves to its new place with a single UPDATE
        await db.InstitutionUnits
            .Where(u => u.Node.IsDescendantOf(oldNode))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Node, u => u.Node.GetReparentedValue(oldNode, newNode)), ct);
        await db.InstitutionUnits
            .Where(u => u.Id == unitId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ParentId, newParentId), ct);

        await tx.CommitAsync(ct);
        return MoveResult.Moved;
    }
}
