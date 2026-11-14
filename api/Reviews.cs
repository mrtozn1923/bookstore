using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace Bookstore.Assistant.Api;

public sealed record Review(string Author, int Rating, string Text);
public sealed record ReviewSummary(string Summary, string Source);

// Demo yorumları; gerçek uygulamada Bookstore.Catalog module'ünden gelir
public static class Reviews
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<Review>> ByIsbn = new Dictionary<string, IReadOnlyList<Review>>
    {
        ["9780441172719"] =
        [
            new("Ada", 5, "The world building is incredible: ecology, religion and politics all fit together."),
            new("Alan", 3, "The first hundred pages are slow and full of unfamiliar terms. It gets much better later."),
            new("Grace", 5, "Paul's story still feels modern. I read it twice."),
            new("Linus", 4, "Great ideas, but the appendix and glossary are almost required reading."),
        ],
        ["9780547928227"] =
        [
            new("Ada", 5, "A warm, funny adventure. Perfect to read aloud to children."),
            new("Grace", 4, "Shorter and lighter than The Lord of the Rings, which is exactly why I liked it."),
            new("Alan", 4, "Bilbo's riddle game with Gollum is the best chapter."),
        ],
        ["9780141439587"] =
        [
            new("Linus", 3, "Witty, but Emma herself is hard to like for the first half."),
            new("Grace", 5, "Austen's humour is sharp. The misunderstandings are a joy to follow."),
            new("Ada", 4, "Slow plot, brilliant dialogue."),
        ],
    };

    public static void MapReviews(this WebApplication app)
    {
        app.MapGet("/reviews/{isbn}", IResult (string isbn) =>
            ByIsbn.TryGetValue(isbn, out IReadOnlyList<Review>? reviews) ? TypedResults.Ok(reviews) : TypedResults.NotFound());

        // Cihazda özetleyemeyen tarayıcılar için sunucu özeti; yorumlar sık değişmediği için cache'lenir
        app.MapGet("/reviews/{isbn}/summary", async Task<IResult> (string isbn, IChatClient chat, IMemoryCache cache,
            ILogger<ReviewSummary> logger, CancellationToken ct) =>
        {
            if (!ByIsbn.TryGetValue(isbn, out IReadOnlyList<Review>? reviews))
            {
                return TypedResults.NotFound();
            }
            ReviewSummary? summary = await cache.GetOrCreateAsync($"review-summary:{isbn}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                string text = string.Join('\n', reviews.Select(r => $"- ({r.Rating}/5) {r.Text}"));
                ChatResponse response = await chat.GetResponseAsync(
                    [
                        new(ChatRole.System, "Summarize customer reviews of a book in two short sentences. " +
                            "Mention what readers liked and what they criticised. Do not invent details."),
                        new(ChatRole.User, text),
                    ], cancellationToken: ct);
                logger.LogInformation("Review summary for {Isbn}: {Tokens} output tokens", isbn, response.Usage?.OutputTokenCount);
                return new ReviewSummary(response.Text.Trim(), "server");
            });
            return TypedResults.Ok(summary);
        }).RequireRateLimiting("chat");
    }
}
