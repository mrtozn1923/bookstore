namespace Bookstore.Api.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "";
    public string ApiKey { get; set; } = "";
}
