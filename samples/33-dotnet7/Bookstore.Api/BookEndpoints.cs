using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api;

public static class BookEndpoints
{
    // The return type itself states which responses this endpoint can produce
    public static Results<Ok<Book>, NotFound> GetById(int id, IBookService service)
    {
        var book = service.GetById(id);
        return book is null ? TypedResults.NotFound() : TypedResults.Ok(book);
    }
}
