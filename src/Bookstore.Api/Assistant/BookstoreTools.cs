using System.Text.Json;
using Anthropic.Models.Messages;
using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;

namespace Bookstore.Api.Assistant;

public sealed class BookstoreTools(IBookRepository books, IOrderRepository orders)
{
    public const string SearchBooks = "search_books";
    public const string GetOrderStatus = "get_order_status";

    public static readonly ToolUnion SearchBooksTool = new Tool
    {
        Name = SearchBooks,
        Description = "Searches the Bookstore catalog by title, author or topic words. Returns up to 5 books with ISBN, title, author and price in TRY. "
                      + "Use it whenever the customer asks for a book recommendation or whether a book is in stock. Do not use it for order questions.",
        Strict = true,
        InputSchema = new InputSchema(new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                query = new { type = "string", description = "Words to search for, for example 'fantasy' or 'Austen'." },
                max_price = new { type = "number", description = "Optional upper price limit in TRY." },
            }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "query" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        }),
    };

    public static readonly ToolUnion GetOrderStatusTool = new Tool
    {
        Name = GetOrderStatus,
        Description = "Returns the status, order date and ISBNs of one order of the signed-in customer. "
                      + "Use it when the customer asks where an order is. It only sees the signed-in customer's own orders.",
        Strict = true,
        InputSchema = new InputSchema(new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                order_id = new { type = "string", description = "The order number, for example '1042'." },
            }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "order_id" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        }),
    };

    public static IReadOnlyList<ToolUnion> Definitions => [SearchBooksTool, GetOrderStatusTool];

    // Author questions: a server tool that Anthropic runs; its results come back in the same response.
    public static readonly ToolUnion AuthorWebSearchTool = new WebSearchTool20260318
    {
        MaxUses = 3,
        AllowedDomains = ["en.wikipedia.org"],
        AllowedCallers = [WebSearchTool20260318AllowedCaller.Direct],
    };

    public string Run(string name, IReadOnlyDictionary<string, JsonElement> input, CurrentCustomer customer) => name switch
    {
        SearchBooks => RunSearch(input),
        GetOrderStatus => RunOrderStatus(input, customer),
        _ => throw new ToolInputException($"Unknown tool '{name}'."),
    };

    public ToolResultBlockParam Execute(ToolUseBlock call, CurrentCustomer customer)
    {
        try
        {
            return new ToolResultBlockParam(call.ID) { Content = Run(call.Name, call.Input, customer) };
        }
        catch (ToolInputException ex)
        {
            // The model gets the error as a result and can correct itself; it is not an exception for the API.
            return new ToolResultBlockParam(call.ID) { Content = ex.Message, IsError = true };
        }
    }

    private string RunSearch(IReadOnlyDictionary<string, JsonElement> input)
    {
        string query = input["query"].GetString() ?? string.Empty;
        decimal? maxPrice = input.TryGetValue("max_price", out JsonElement price) ? price.GetDecimal() : null;

        var found = books.Search(query, maxPrice).Take(5)
            .Select(b => new { isbn = b.Isbn, title = b.Title, author = b.Author, price_try = b.Price });
        return JsonSerializer.Serialize(found);
    }

    private string RunOrderStatus(IReadOnlyDictionary<string, JsonElement> input, CurrentCustomer customer)
    {
        string orderId = input["order_id"].GetString() ?? string.Empty;
        Order? order = orders.Find(orderId);

        // The authorization check lives in code, not in the prompt: another customer's order looks like a missing one.
        if (order is null || order.CustomerId != customer.Id)
        {
            throw new ToolInputException($"No order {orderId} was found for this customer. Ask the customer to check the number.");
        }

        return JsonSerializer.Serialize(new { order_id = order.Id, status = order.Status, placed_at_utc = order.PlacedAtUtc, isbns = order.Isbns });
    }
}

public sealed class ToolInputException(string message) : Exception(message);
