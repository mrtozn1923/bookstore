using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Bookstore.Assistant.Api;

public sealed record BookCard(string Isbn, string Title, string Author, decimal PriceTry);
public sealed record BookListProps(string Heading, IReadOnlyList<BookCard> Books);

// Demo: oturumdaki müşterinin siparişleri; gerçek uygulamada Bookstore.Ordering module'ünden gelir
public sealed class OrderBook
{
    public ConcurrentDictionary<int, string> Statuses { get; } = new() { [1042] = "Processing", [1043] = "Shipped" };
}

public sealed class AssistantTools(OrderBook orders, ILogger<AssistantTools> logger)
{
    // UI tool'ları: sonucu metin olarak değil, bir component olarak gösterilir
    public static readonly IReadOnlyDictionary<string, string> UiComponents =
        new Dictionary<string, string> { ["show_books"] = "book-list" };

    public ChatOptions CreateOptions() => new()
    {
        Tools =
        [
            AIFunctionFactory.Create(ShowBooks, "show_books"),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(CancelOrder, "cancel_order")),
        ],
    };

    [Description("Shows books from the catalog to the customer as cards with an 'Add to cart' button. " +
        "Use this to recommend books instead of listing them in text.")]
    public object ShowBooks(
        [Description("Optional genre: science-fiction, fantasy, classic or crime.")] string? genre = null,
        [Description("Optional maximum price in TRY.")] decimal? maxPriceTry = null,
        [Description("Optional short heading for the cards.")] string heading = "Recommended books")
    {
        // Model yalnız filtre seçer; kitap, yazar ve fiyat her zaman katalogdan gelir
        if (genre is not null && !Catalog.Books.Any(b => b.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase)))
        {
            return new { error = $"Unknown genre '{genre}'. Use science-fiction, fantasy, classic or crime." };
        }
        List<BookCard> books = [.. Catalog.Books
            .Where(b => genre is null || b.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase))
            .Where(b => maxPriceTry is null || b.PriceTry <= maxPriceTry)
            .Take(6)
            .Select(b => new BookCard(b.Isbn, b.Title, b.Author, b.PriceTry))];
        logger.LogInformation("show_books(genre={Genre}, maxPriceTry={Max}) -> {Count} cards", genre, maxPriceTry, books.Count);
        return new BookListProps(heading.Length > 80 ? heading[..80] : heading, books);
    }

    [Description("Cancels one of the current customer's orders. Requires the customer's approval.")]
    public object CancelOrder([Description("The order number, for example 1042.")] int orderId)
    {
        if (!orders.Statuses.TryGetValue(orderId, out string? status))
        {
            return new { error = $"Order {orderId} was not found for this customer." };
        }
        if (status == "Shipped")
        {
            return new { error = $"Order {orderId} has already shipped and cannot be cancelled." };
        }
        orders.Statuses[orderId] = "Cancelled";
        logger.LogInformation("cancel_order({OrderId}): cancelled", orderId);
        return new { orderId, status = "Cancelled" };
    }
}
