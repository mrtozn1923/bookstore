using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Support;

public sealed record InvoiceAnswer(string Text, ReplyOutcome Outcome, long CountedInputTokens, TokenUsage Usage);

public sealed class InvoiceAnalyzer(IAnthropicClient claude, IOptions<ClaudeOptions> options)
{
    public const long MaxInputTokens = 50_000;

    public static List<ContentBlockParam> BuildContent(byte[] pdf, string question) =>
    [
        // The documentation recommends putting the PDF before the text.
        new DocumentBlockParam { Source = new Base64PdfSource { Data = Convert.ToBase64String(pdf) } },
        new TextBlockParam($"This is a Bookstore order invoice. {question} Answer only from the invoice."),
    ];

    public async Task<InvoiceAnswer?> AnalyzeAsync(byte[] pdf, string question, CancellationToken cancellationToken)
    {
        ClaudeOptions settings = options.Value;
        List<ContentBlockParam> content = BuildContent(pdf, question);

        // count_tokens is free and has its own rate limit; it is an estimate.
        MessageTokensCount count = await claude.Messages.CountTokens(new MessageCountTokensParams
        {
            Model = settings.Model,
            Messages = [new UserMessageParam { Content = content }],
        }, cancellationToken);

        if (count.InputTokens > MaxInputTokens)
        {
            return null;
        }

        Message message = await claude.Messages.Create(new MessageCreateParams
        {
            Model = settings.Model,
            MaxTokens = settings.MaxTokens,
            Messages = [new UserMessageParam { Content = content }],
        }, cancellationToken);

        ClaudeReply reply = ClaudeReplyReader.Read(message);
        return new InvoiceAnswer(reply.Text, reply.Outcome, count.InputTokens, reply.Usage);
    }
}
