using Bookstore.Web.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.ViewComponents;

public sealed class CategoryMenuViewComponent(IBookCatalog catalog) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await catalog.CategoriesAsync(HttpContext.RequestAborted));
}
