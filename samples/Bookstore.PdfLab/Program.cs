using System.Diagnostics;
using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Samples;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

string command = args.FirstOrDefault() ?? "invoice";
Directory.CreateDirectory("out");

if (command != "no-license")
{
    // Bookstore qualifies for the Community License (see the post); this line states it
    QuestPDF.Settings.License = LicenseType.Community;
}

if (args.Contains("--system-fonts"))
{
    QuestPDF.Settings.UseSystemFonts = true;
}

switch (command)
{
    case "no-license":
    case "invoice":
    {
        InvoiceModel invoice = BookstoreSampleData.Invoice(1042);
        Stopwatch watch = Stopwatch.StartNew();
        await using (FileStream file = File.Create("out/invoice.pdf"))
        {
            await new InvoicePdfRenderer().RenderAsync(invoice, file);
        }

        Console.WriteLine($"out/invoice.pdf {new FileInfo("out/invoice.pdf").Length / 1024} KB in {watch.ElapsedMilliseconds} ms");
        PrintText("out/invoice.pdf");
        break;
    }
    case "long":
    {
        int lines = int.Parse(args[1]);
        await using (FileStream file = File.Create("out/invoice-long.pdf"))
        {
            await new InvoicePdfRenderer().RenderAsync(BookstoreSampleData.LongInvoice(1043, lines), file);
        }

        using PdfDocument pdf = PdfDocument.Open("out/invoice-long.pdf");
        foreach (Page page in pdf.GetPages())
        {
            string text = string.Join(" ", page.GetWords().Select(w => w.Text));
            Console.WriteLine($"page {page.Number}: header row={text.Contains("ISBN Title Qty")} lines={CountIsbnRows(text)} footer='{text[text.LastIndexOf("Page", StringComparison.Ordinal)..]}'");
        }

        break;
    }
    case "font":
    {
        // A font family that is not shipped with the app
        InvoiceModel invoice = BookstoreSampleData.Invoice(1042);
        try
        {
            Document.Create(c => c.Page(p => p.Content().Text(invoice.BillingAddress.Line1).FontFamily(args[1]))).GeneratePdf("out/font.pdf");
            PrintText("out/font.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }

        break;
    }
    case "glyphs":
    {
        // A title the bundled Lato font cannot draw
        try
        {
            Document.Create(c => c.Page(p => p.Content().Text("Haruki Murakami: 海辺のカフカ"))).GeneratePdf("out/glyphs.pdf");
            PrintText("out/glyphs.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }

        break;
    }
    case "timing":
    {
        InvoicePdfRenderer renderer = new();
        await renderer.RenderAsync(BookstoreSampleData.Invoice(1), Stream.Null);   // warm-up
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            await renderer.RenderAsync(BookstoreSampleData.Invoice(2000 + i), Stream.Null);
        }

        Console.WriteLine($"100 invoices: {watch.ElapsedMilliseconds} ms");
        break;
    }
    case "image":
    {
        int index = 0;
        foreach (byte[] png in InvoicePdfRenderer.CreateDocument(BookstoreSampleData.Invoice(1042))
                     .GenerateImages(new QuestPDF.Infrastructure.ImageGenerationSettings { RasterDpi = 218 }))
        {
            await File.WriteAllBytesAsync($"out/invoice-page{++index}.png", png);
        }

        Console.WriteLine($"{index} page image(s)");
        break;
    }
}

static void PrintText(string path)
{
    using PdfDocument pdf = PdfDocument.Open(path);
    Console.WriteLine($"pages={pdf.NumberOfPages}");
    foreach (Page page in pdf.GetPages())
    {
        foreach (string line in page.GetWords().GroupBy(w => Math.Round(w.BoundingBox.Bottom)).OrderByDescending(g => g.Key)
                     .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))))
        {
            Console.WriteLine("  " + line);
        }
    }
}

static int CountIsbnRows(string text) => text.Split(' ').Count(w => w.Length == 13 && w.StartsWith("978", StringComparison.Ordinal));
