using Anthropic.Models.Messages;
using Anthropic.Models.Messages.Batches;

namespace Bookstore.Api.Catalog.Seo;

public static class BookSeoBatch
{
    public const string Model = "claude-haiku-5-5";

    // The documented limit is 100,000 requests or 256 MB per batch; smaller batches are easier to watch and retry.
    public const int MaxRequestsPerBatch = 10_000;

    private const string CustomIdPrefix = "seo-";

    public const string SystemPrompt = """
        You write meta descriptions for the book pages of Bookstore, an online bookstore.
        Write one English description of 140 to 160 characters. Use only the book data you are given.
        Return only the description, without quotes.
        """;

    // custom_id must match ^[a-zA-Z0-9_-]{1,64}$. Results come back in any order and are matched by this id.
    public static string CustomIdFor(Book book) => CustomIdPrefix + book.Isbn;

    public static string? IsbnFrom(string customId) =>
        customId.StartsWith(CustomIdPrefix, StringComparison.Ordinal) ? customId[CustomIdPrefix.Length..] : null;

    public static Request BuildRequest(Book book) => new()
    {
        CustomID = CustomIdFor(book),
        Params = new()
        {
            Model = Model,
            MaxTokens = 300,
            System = SystemPrompt,
            // A short, well-defined task: no thinking needed on Haiku 5.5.
            Thinking = new ThinkingConfigDisabled(),
            Messages = [new UserMessageParam { Content = $"Title: {book.Title}\nAuthor: {book.Author}\nAbout: {book.Description}" }],
        },
    };

    public static IEnumerable<BatchCreateParams> Split(IEnumerable<Book> books) =>
        books.Chunk(MaxRequestsPerBatch)
             .Select(chunk => new BatchCreateParams { Requests = chunk.Select(BuildRequest).ToList() });
}
