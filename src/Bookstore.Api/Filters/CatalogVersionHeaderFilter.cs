using Microsoft.AspNetCore.Mvc.Filters;

namespace Bookstore.Api.Filters;

public class CatalogVersionHeaderFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        context.HttpContext.Response.Headers["X-Catalog-Version"] = "1";
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
