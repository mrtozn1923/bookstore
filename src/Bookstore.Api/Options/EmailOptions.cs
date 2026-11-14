using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required, EmailAddress]
    public string FromAddress { get; set; } = "";

    [Required]
    public string ApiKey { get; set; } = "";
}
