using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Catalog;

public sealed class ClaudeCatalogService(
    IAnthropicClient claude,
    IOptions<ClaudeOptions> options,
    ILogger<ClaudeCatalogService> logger)
{
    private const string SystemPrompt = """
        You write catalog summaries for Bookstore, an online bookstore.
        Write exactly two sentences in Turkish. No spoilers, no marketing superlatives.
        Use only the facts in the description; do not invent awards or sales figures.
        """;

    public static MessageCreateParams BuildRequest(Book book, ClaudeOptions settings) => new()
    {
        Model = settings.Model,
        MaxTokens = settings.MaxTokens,
        System = SystemPrompt,
        Messages =
        [
            new UserMessageParam
            {
                Content = $"Title: {book.Title}\nAuthor: {book.Author}\nDescription: {book.Description}",
            },
        ],
    };

    public async Task<BookSummary?> SummarizeAsync(Book book, CancellationToken cancellationToken)
    {
        MessageCreateParams parameters = BuildRequest(book, options.Value);

        try
        {
            Message message = await claude.Messages.Create(parameters, cancellationToken);

            // content is a list of blocks; join only the text blocks.
            string summary = string.Concat(message.Content
                .Select(block => block.TryPickText(out TextBlock? text) ? text.Text : string.Empty));

            logger.LogInformation(
                "Summary for {Isbn}: model={Model} stop={StopReason} input={InputTokens} output={OutputTokens}",
                book.Isbn, message.Model.Raw(), message.StopReason?.Raw(),
                message.Usage.InputTokens, message.Usage.OutputTokens);

            return new BookSummary(book.Isbn, summary.Trim(), message.Model.Raw(),
                message.Usage.InputTokens, message.Usage.OutputTokens);
        }
        catch (AnthropicApiException ex)
        {
            // A failed call does not crash the request; the endpoint returns 503.
            logger.LogWarning(ex, "Claude call failed for {Isbn}", book.Isbn);
            return null;
        }
    }
}
