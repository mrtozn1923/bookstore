namespace Bookstore.Prompts.Tasks.OrderExtraction;

public sealed record OrderEmail(DateOnly Sent, string Body)
{
    // The model cannot know "today"; the sent date travels with the email
    public string ToPromptText() =>
        $"Sent: {Sent:yyyy-MM-dd} ({Sent.DayOfWeek})\n\n{Body}";
}

public sealed record ExpectedOrder(DateOnly Sent, List<ExpectedItem> Items, DateOnly? DeliveryDate, bool DateAmbiguous = false);

public sealed record ExpectedItem(string Title, string? Isbn, int? Quantity);
