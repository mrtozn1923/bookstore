using Bookstore.Data;

namespace Bookstore.Privacy.Tests;

public sealed class InventoryTests
{
    [Fact]
    public void Every_mapped_property_is_classified()
    {
        using var testDb = new TestDb();
        using BookstoreContext db = testDb.CreateContext();

        string[] missing = [.. PersonalDataInventory.UnclassifiedProperties(db)];

        Assert.True(missing.Length == 0,
            "Classify these properties or remove them: " + string.Join(", ", missing));
    }

    [Fact]
    public void Every_table_belongs_to_a_processing_activity()
    {
        using var testDb = new TestDb();
        using BookstoreContext db = testDb.CreateContext();

        IReadOnlyList<InventoryEntry> inventory = PersonalDataInventory.Build(db);

        Assert.All(inventory, e => Assert.False(string.IsNullOrWhiteSpace(e.Activity.Retention)));
    }
}
