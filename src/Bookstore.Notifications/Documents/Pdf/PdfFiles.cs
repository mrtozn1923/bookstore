using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;

namespace Bookstore.Notifications.Documents.Pdf;

// Reading and merging existing PDFs with PdfPig (Apache 2.0)
public static class PdfFiles
{
    public static byte[] Merge(params byte[][] documents) => PdfMerger.Merge(documents);

    public static IReadOnlyList<string> PageTexts(byte[] pdf)
    {
        using PdfDocument document = PdfDocument.Open(pdf);
        return [.. document.GetPages().Select(page => string.Join(" ", page.GetWords().Select(w => w.Text)))];
    }
}
