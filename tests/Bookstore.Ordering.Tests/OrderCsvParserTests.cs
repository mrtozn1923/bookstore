using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class OrderCsvParserTests
{
    [Fact]
    public void Parse_skips_header_and_blank_lines_and_keeps_row_numbers()
    {
        IReadOnlyList<CsvRow> rows = OrderCsvParser.Parse("customer_id;isbn;quantity;unit_price\r\n\r\n7;9780441013593;1;12,45\r\n");

        CsvRow row = Assert.Single(rows);
        Assert.Equal(3, row.Number);
        Assert.Equal(["7", "9780441013593", "1", "12,45"], row.Fields);
    }
}
