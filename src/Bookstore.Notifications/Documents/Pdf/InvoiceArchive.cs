namespace Bookstore.Notifications.Documents.Pdf;

// An issued invoice never changes: render it once, keep the file, serve the same bytes on every download
public sealed class InvoiceArchive(IDocumentRenderer<InvoiceModel> renderer, IFileStore files)
{
    public static string PathFor(InvoiceModel invoice) =>
        $"invoices/{invoice.IssueDate:yyyy}/{invoice.InvoiceNumber}{FileFormat.Pdf.Extension}";

    public Task<bool> ExistsAsync(InvoiceModel invoice, CancellationToken cancellationToken = default) =>
        files.ExistsAsync(PathFor(invoice), cancellationToken);

    public async Task<string> EnsureStoredAsync(InvoiceModel invoice, CancellationToken cancellationToken = default)
    {
        string path = PathFor(invoice);
        if (await files.ExistsAsync(path, cancellationToken))
        {
            return path;
        }

        using MemoryStream pdf = new();
        await renderer.RenderAsync(invoice, pdf, cancellationToken);
        pdf.Position = 0;
        await files.SaveAsync(path, pdf, renderer.Format.ContentType, cancellationToken);
        return path;
    }
}
