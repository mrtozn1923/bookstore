using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class OrderCsvImporterTests
{
    private const string Csv =
        "customer_id;isbn;quantity;unit_price\r\n" +
        "7;978-0-441-01359-3;1;12,45\r\n" +
        "7;9780261103344;2;1.149,90\r\n" +
        "8;9780141439587;1;59.90\r\n" +
        "9;9780141439549;0;129,90\r\n" +
        "9;9780141439548;1;129,90\r\n" +
        "x;9780141439549;1;129,90\r\n" +
        "10;9780141439549;1\r\n" +
        "11;9780441013593;3;abc\r\n" +
        "12;9780441013593;1;1,234.50\r\n" +
        "\r\n";

    [Fact]
    public Task Import_reports_lines_and_errors() =>
        Verify(new OrderCsvImporter().Import(Csv));
}
