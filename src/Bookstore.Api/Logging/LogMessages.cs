namespace Bookstore.Api.Logging;

public static partial class LogMessages
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Book {BookId} not found")]
    public static partial void BookNotFound(ILogger logger, int bookId);
}
