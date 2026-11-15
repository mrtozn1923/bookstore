using System.Diagnostics;
using System.IO.Compression;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Documents.Excel;
using Bookstore.Notifications.Samples;
using ClosedXML.Excel;

string command = args.FirstOrDefault() ?? "report";
Directory.CreateDirectory("out");
DateOnly month = new(2026, 10, 1);

switch (command)
{
    case "report":
    {
        MonthlySalesReport report = BookstoreSampleData.MonthlySales(month, rowCount: 2_000);
        await using (FileStream file = File.Create("out/monthly-sales-2026-10.xlsx"))
        {
            await new MonthlySalesExcelRenderer().RenderAsync(report, file);
        }

        // Read the file back: types of the first data row and the formulas' results
        using XLWorkbook workbook = new("out/monthly-sales-2026-10.xlsx");
        IXLWorksheet sales = workbook.Worksheet("Sales");
        foreach (string address in new[] { "A2", "B2", "D2", "G2", "H2" })
        {
            IXLCell cell = sales.Cell(address);
            Console.WriteLine($"{address,-3} {cell.DataType,-8} {cell.Value,-22} format='{cell.Style.NumberFormat.Format}' formula='{cell.FormulaA1}'");
        }

        IXLWorksheet summary = workbook.Worksheet("Summary");
        foreach (IXLRow row in summary.RowsUsed().Skip(1))
        {
            Console.WriteLine($"{row.Cell(1).GetString(),-22} {row.Cell(2).Value,6} {row.Cell(3).Value,12}");
        }

        Console.WriteLine($"rows={report.Rows.Count} sum(Total)={report.Rows.Sum(r => r.Total):0.00}");
        break;
    }
    case "injection":
    {
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Sales");
        sheet.Cell("A1").Value = "=HYPERLINK(\"https://attacker.example.com\",\"Dune\")";
        MonthlySalesExcelRenderer.SetText(sheet.Cell("A2"), "=HYPERLINK(\"https://attacker.example.com\",\"Dune\")");
        workbook.SaveAs("out/injection.xlsx");
        Console.WriteLine($"A1 DataType={sheet.Cell("A1").DataType} HasFormula={sheet.Cell("A1").HasFormula}");

        using ZipArchive zip = ZipFile.OpenRead("out/injection.xlsx");
        string xml = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open()).ReadToEnd();
        int start = xml.IndexOf("<x:row ");
        Console.WriteLine(xml[start..(xml.IndexOf("</x:sheetData>"))]);
        string styles = new StreamReader(zip.GetEntry("xl/styles.xml")!.Open()).ReadToEnd();
        int xf = styles.IndexOf("quotePrefix");
        Console.WriteLine(xf >= 0 ? styles[(styles.LastIndexOf("<x:xf ", xf))..(styles.IndexOf("/>", xf) + 2)] : "no quotePrefix");
        break;
    }
    case "large":
    {
        string engine = args[1];
        int count = int.Parse(args[2]);
        // ClosedXML needs the whole list; the streaming writer gets rows one by one
        MonthlySalesReport? report = engine == "closedxml" ? BookstoreSampleData.MonthlySales(month, count) : null;
        GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        long peak = 0;
        using CancellationTokenSource stop = new();
        Task sampler = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                peak = Math.Max(peak, GC.GetTotalMemory(false));
                await Task.Delay(10);
            }
        });

        long allocatedBefore = GC.GetTotalAllocatedBytes(true);
        Stopwatch watch = Stopwatch.StartNew();
        string path = $"out/large-{engine}.xlsx";
        await using (FileStream file = new(path, FileMode.Create, args.Contains("--write-only") ? FileAccess.Write : FileAccess.ReadWrite))
        {
            if (engine == "closedxml")
            {
                await new MonthlySalesExcelRenderer().RenderAsync(report!, file);
            }
            else if (engine == "spreadcheetah")
            {
                await SpreadCheetahExport.WriteAsync(BookstoreSampleData.MonthlySalesStream(month, count), file);
            }
            else
            {
                LargeSalesExportWriter.Write(BookstoreSampleData.MonthlySalesStream(month, count), file);
            }
        }

        watch.Stop();
        stop.Cancel();
        await sampler;
        Console.WriteLine($"{engine,-9} rows={count,7} time={watch.ElapsedMilliseconds,6} ms  heap peak above baseline={(peak - baseline) / 1024 / 1024,5} MB  allocated={(GC.GetTotalAllocatedBytes(true) - allocatedBefore) / 1024 / 1024,6} MB  file={new FileInfo(path).Length / 1024,6} KB");
        break;
    }
    case "verify":
    {
        // Open the exported file with a different library and check rows and types
        using XLWorkbook workbook = new(args[1]);
        IXLWorksheet sheet = workbook.Worksheet(1);
        IXLRow second = sheet.Row(2);
        Console.WriteLine($"{Path.GetFileName(args[1])}: rows={sheet.LastRowUsed()!.RowNumber() - 1} B2={second.Cell(2).DataType} D2={second.Cell(4).DataType}:{second.Cell(4).GetString()} H2={second.Cell(8).DataType}:{second.Cell(8).Value} format='{second.Cell(8).Style.NumberFormat.Format}'");
        break;
    }
    case "template":
    {
        IReadOnlyList<CatalogEntry> catalog = [.. BookstoreSampleData.Books.Select(b => new CatalogEntry(b.Isbn, b.Title, b.Price))];
        await using (FileStream file = File.Create("out/institutional-order.xlsx"))
        {
            InstitutionalOrderTemplate.Create(file, "Hilltop Primary School", ["Science Department", "Library", "Grade 4"], catalog);
        }

        // Fill it the way a customer might: one ISBN pasted as a number, one quantity typed as text
        using (XLWorkbook workbook = new("out/institutional-order.xlsx"))
        {
            IXLWorksheet order = workbook.Worksheet("Order");
            order.Cell("A6").Value = "9780441172719"; order.Cell("C6").Value = "Science Department"; order.Cell("D6").Value = 25;
            order.Cell("A7").Value = 9780547928227;   order.Cell("C7").Value = "Library";            order.Cell("D7").Value = 30;
            order.Cell("A8").Value = "9780141439587"; order.Cell("C8").Value = "Grade 4";            order.Cell("D8").Value = "20 copies";
            workbook.SaveAs("out/institutional-order-filled.xlsx");
        }

        await using FileStream filled = File.OpenRead("out/institutional-order-filled.xlsx");
        InstitutionalOrderSheet sheet = InstitutionalOrderTemplate.Read(filled);
        foreach (InstitutionalOrderLine line in sheet.Lines)
        {
            Console.WriteLine($"row {line.Row}: {line.Isbn} x{line.Quantity} for {line.Unit}");
        }

        foreach (string problem in sheet.Problems)
        {
            Console.WriteLine(problem);
        }

        using XLWorkbook check = new("out/institutional-order-filled.xlsx");
        IXLWorksheet o = check.Worksheet("Order");
        Console.WriteLine($"B6='{o.Cell("B6").Value}' E6={o.Cell("E6").Value} F6={o.Cell("F6").Value} protected={o.IsProtected} A6 locked={o.Cell("A6").Style.Protection.Locked} B6 locked={o.Cell("B6").Style.Protection.Locked}");
        break;
    }
}
