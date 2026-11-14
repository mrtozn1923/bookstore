using Bookstore.Inventory;

namespace Bookstore.Inventory.Tests;

[TestFixture]
public class StockExportTests
{
    [Test]
    public void Legacy_export_is_written_in_the_turkish_code_page()
    {
        byte[] file = StockCsvExport.ToLegacyCsv([new StockItem("9780441172719", "Çatalca", 12, 2)]);

        string expected = "isbn;warehouse;available\n9780441172719;Çatalca;10\n";
        Assert.That(file, Has.Length.EqualTo(expected.Length));
        Assert.That(file, Has.Member((byte)0xC7));
    }
}
