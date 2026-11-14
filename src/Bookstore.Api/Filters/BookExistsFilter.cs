using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Bookstore.Api.Data;

namespace Bookstore.Api.Filters;

public class BookExistsFilter : IAsyncActionFilter
{
    private readonly BookstoreContext _db;

    public BookExistsFilter(BookstoreContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.TryGetValue("id", out object? value) && value is int id)
        {
            bool exists = await _db.Books.AnyAsync(b => b.Id == id, context.HttpContext.RequestAborted);
            if (!exists)
            {
                // setting Result short-circuits the pipeline; the action never runs
                context.Result = new NotFoundObjectResult(new { message = $"Book {id} does not exist." });
                return;
            }
        }

        await next();
    }
}
