namespace Bookstore.Web.Errors;

// A business rule the user can understand and fix; UserMessage is safe to show on a page.
public abstract class DomainException(string userMessage, int statusCode) : Exception(userMessage)
{
    public string UserMessage { get; } = userMessage;
    public int StatusCode { get; } = statusCode;
}
