namespace Bookstore.Institutional.Domain;

public class OrderAttachment
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string StoragePath { get; set; } = "";
}
