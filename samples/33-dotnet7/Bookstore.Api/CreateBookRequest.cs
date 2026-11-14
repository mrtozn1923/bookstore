namespace Bookstore.Api;

public class CreateBookRequest
{
    public required string Title { get; init; }
    public required decimal Price { get; init; }
}
