using Bookstore.Web.Errors;

namespace Bookstore.Web.Ordering;

public sealed class OutOfStockException(string title)
    : DomainException($"\"{title}\" is out of stock right now.", StatusCodes.Status409Conflict);
