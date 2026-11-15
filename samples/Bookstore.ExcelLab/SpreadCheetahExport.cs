using Bookstore.Notifications.Documents;
using SpreadCheetah;
using SpreadCheetah.Styling;

// Fully streaming alternative (MIT): rows are compressed and written as they come
public static class SpreadCheetahExport
{
    public static async Task WriteAsync(IEnumerable<SalesRow> rows, Stream output, CancellationToken cancellationToken = default)
    {
        await using Spreadsheet spreadsheet = await Spreadsheet.CreateNewAsync(output, cancellationToken: cancellationToken);
        await spreadsheet.StartWorksheetAsync("Sales", token: cancellationToken);
        StyleId date = spreadsheet.AddStyle(new Style { Format = NumberFormat.Custom("yyyy-mm-dd hh:mm") });
        StyleId money = spreadsheet.AddStyle(new Style { Format = NumberFormat.Custom("#,##0.00 \"₺\"") });

        await spreadsheet.AddRowAsync(
            [new("Order"), new("Placed at (UTC)"), new("Customer"), new("ISBN"), new("Title"), new("Quantity"), new("Unit price"), new("Total")],
            cancellationToken);

        Cell[] cells = new Cell[8];
        foreach (SalesRow row in rows)
        {
            cells[0] = new Cell(row.OrderId);
            cells[1] = new Cell(row.PlacedAtUtc, date);
            cells[2] = new Cell(row.CustomerEmail);
            cells[3] = new Cell(row.Isbn);
            cells[4] = new Cell(row.Title);
            cells[5] = new Cell(row.Quantity);
            cells[6] = new Cell(row.UnitPrice, money);
            cells[7] = new Cell(row.Total, money);
            await spreadsheet.AddRowAsync(cells, cancellationToken);
        }

        await spreadsheet.FinishAsync(cancellationToken);
    }
}
