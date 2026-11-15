using System.Text;

namespace Bookstore.Prompts.Tasks.ReportSql;

public sealed record ReportColumn(string Name, string Type, string Note = "");

public sealed record ReportTable(string Name, IReadOnlyList<ReportColumn> Columns);

// The reporting user can read only these views in the reporting schema
public static class ReportingSchema
{
    public const string Schema = "reporting";

    public static readonly IReadOnlyList<string> Parameters = ["@StartDate", "@EndDate"];

    public static readonly IReadOnlyList<ReportTable> Tables =
    [
        new("Books", [new("Id", "int"), new("Isbn", "char(13)"), new("Title", "nvarchar(200)"),
            new("AuthorId", "int", "references Authors.Id"), new("CategoryId", "int", "references Categories.Id"),
            new("Price", "decimal(10,2)", "TRY"), new("PublishedOn", "date")]),
        new("Authors", [new("Id", "int"), new("Name", "nvarchar(200)")]),
        new("Categories", [new("Id", "int"), new("Name", "nvarchar(100)")]),
        new("Customers", [new("Id", "int"), new("Name", "nvarchar(200)"), new("City", "nvarchar(100)"),
            new("CreatedAt", "datetime2", "UTC")]),
        new("Orders", [new("Id", "int"), new("CustomerId", "int", "references Customers.Id"),
            new("OrderedAt", "datetime2", "UTC"),
            new("Status", "varchar(20)", "'Pending', 'Shipped', 'Delivered', 'Cancelled' or 'Returned'"),
            new("IsInstitutional", "bit", "1 for school, library and company orders"),
            new("Total", "decimal(12,2)", "TRY, after discounts")]),
        new("OrderItems", [new("Id", "int"), new("OrderId", "int", "references Orders.Id"),
            new("BookId", "int", "references Books.Id"), new("Quantity", "int"), new("UnitPrice", "decimal(10,2)", "TRY")]),
        new("StockItems", [new("BookId", "int", "references Books.Id; one row per book and warehouse"),
            new("WarehouseId", "int"), new("Quantity", "int")]),
    ];

    public static ReportTable? Find(string name) =>
        Tables.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static bool HasColumn(ReportTable table, string column) =>
        table.Columns.Any(c => c.Name.Equals(column, StringComparison.OrdinalIgnoreCase));

    // The schema goes into the prompt as DDL; the same list drives the checks in code
    public static string Ddl()
    {
        StringBuilder ddl = new();
        foreach (ReportTable table in Tables)
        {
            ddl.AppendLine($"CREATE VIEW {Schema}.{table.Name} (");
            ddl.AppendLine(string.Join(",\n", table.Columns.Select(c =>
                $"    {c.Name} {c.Type}" + (c.Note.Length > 0 ? $" -- {c.Note}" : ""))));
            ddl.AppendLine(");");
        }
        return ddl.ToString().TrimEnd();
    }
}
