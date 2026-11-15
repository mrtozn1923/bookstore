namespace Bookstore.Api;

// What the Angular ErrorHandler sends. No user id, no form values, no request bodies.
public sealed record ClientErrorReport(string Message, string Url, string? Stack)
{
    // Query keys that never carry personal data on Bookstore pages; everything else is dropped.
    public static readonly IReadOnlySet<string> AllowedQueryKeys = new HashSet<string> { "page", "category" };
}
