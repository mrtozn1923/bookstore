namespace Bookstore.Notifications;

public sealed record FileFormat(string ContentType, string Extension)
{
    public static readonly FileFormat Pdf = new("application/pdf", ".pdf");
    public static readonly FileFormat Xlsx = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx");
    public static readonly FileFormat PlainText = new("text/plain; charset=utf-8", ".txt");
}

public interface IDocumentRenderer<in TModel>
{
    FileFormat Format { get; }

    Task RenderAsync(TModel model, Stream output, CancellationToken cancellationToken = default);
}
