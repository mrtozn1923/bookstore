using System.Diagnostics;
using Bookstore.Institutional.Hierarchy;
using Microsoft.EntityFrameworkCore;

string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";
const int UnitCount = 1200;
const int ManagerUnitId = 2;   // Purchasing
var counter = new QueryCounter();

await using (var setup = new BookstoreContext(cs, counter))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    // a four-way tree: the children of unit p are 4p-2 .. 4p+1; paths use ids (/1/2/6/)
    var nodes = new Dictionary<int, HierarchyId> { [1] = HierarchyId.Parse("/1/") };
    for (int id = 1; id <= UnitCount; id++)
    {
        int? parent = id == 1 ? null : (id + 2) / 4;
        if (parent is int p)
        {
            nodes[id] = HierarchyId.Parse($"{nodes[p]}{id}/");
        }
        string name = id switch { 1 => "Head Office", 2 => "Purchasing", 3 => "Accounting", _ => $"Unit {id}" };
        setup.InstitutionUnits.Add(new InstitutionUnit { Id = id, InstitutionId = 3, ParentId = parent, Name = name, Node = nodes[id] });
        for (int u = 1; u <= 10; u++)
        {
            setup.InstitutionUsers.Add(new InstitutionUser { InstitutionUnitId = id, FullName = $"User {id}-{u}" });
        }
    }
    await setup.SaveChangesAsync();
}

await using var db = new BookstoreContext(cs, counter);
await db.InstitutionUnits.CountAsync();   // warm-up: open the connection and compile the model

// 1) N+1: walk the tree in the application
counter.Count = 0;
var sw = Stopwatch.StartNew();
List<int> ids = [];
async Task Walk(int id)
{
    ids.Add(id);
    List<int> children = await db.InstitutionUnits.Where(u => u.ParentId == id).Select(u => u.Id).ToListAsync();
    foreach (int child in children)
    {
        await Walk(child);
    }
}
await Walk(ManagerUnitId);
int users = await db.InstitutionUsers.CountAsync(u => ids.Contains(u.InstitutionUnitId));
Console.WriteLine($"N+1 walk:        {ids.Count} units, {users} users, {counter.Count} queries, {sw.ElapsedMilliseconds} ms");

// 2) hierarchyid: the subtree in SQL
counter.Count = 0;
sw.Restart();
HierarchyId root = await db.InstitutionUnits.Where(u => u.Id == ManagerUnitId).Select(u => u.Node).SingleAsync();
IQueryable<InstitutionUser> query = db.InstitutionUsers.Where(u => u.InstitutionUnit.Node.IsDescendantOf(root));
int users2 = await query.CountAsync();
Console.WriteLine($"hierarchyid:     {users2} users, {counter.Count} queries, {sw.ElapsedMilliseconds} ms");
Console.WriteLine(query.ToQueryString());

// 3) the whole tree in one query, built in memory
counter.Count = 0;
sw.Restart();
List<UnitNode> flat = await db.InstitutionUnits.AsNoTracking()
    .Select(u => new UnitNode(u.Id, u.ParentId, u.Name)).ToListAsync();
IReadOnlyList<UnitNode> roots = UnitTree.Build(flat);
Console.WriteLine($"Tree build:      {flat.Count} nodes, {roots.Count} root(s), root has {roots[0].Children.Count} children, {counter.Count} query, {sw.ElapsedMilliseconds} ms");

// 4) lazy loading: the children of one node with a HasChildren flag
int parentId = ManagerUnitId;
List<UnitChildDto> children = await db.InstitutionUnits
    .Where(u => u.ParentId == parentId)
    .OrderBy(u => u.Name)
    .Select(u => new UnitChildDto(u.Id, u.Name, db.InstitutionUnits.Any(c => c.ParentId == u.Id)))
    .ToListAsync();
Console.WriteLine($"Children of {parentId}: {string.Join(", ", children.Select(c => $"{c.Id}:{c.Name}:{(c.HasChildren ? "+" : "-")}"))}");

// 5) moves
var mover = new UnitMoveService(db);
Console.WriteLine($"Move 2 under 6: {await mover.MoveAsync(2, 6)}");
Console.WriteLine($"Move 6 under 3: {await mover.MoveAsync(6, 3)}");
db.ChangeTracker.Clear();
InstitutionUnit moved = await db.InstitutionUnits.AsNoTracking().SingleAsync(u => u.Id == 6);
int subtree = await db.InstitutionUnits.CountAsync(u => u.Node.IsDescendantOf(moved.Node));
Console.WriteLine($"Unit 6 now: node {moved.Node}, parent {moved.ParentId}, subtree size {subtree}");
