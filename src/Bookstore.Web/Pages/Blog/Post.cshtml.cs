using Bookstore.Web.Content;
using Bookstore.Web.StructuredData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bookstore.Web.Pages.Blog;

public sealed class PostModel(PostCatalog catalog) : PageModel
{
    public Post Post { get; private set; } = default!;
    public Author Author { get; private set; } = default!;
    public string Lang { get; private set; } = "tr";

    public IActionResult OnGet(string slug)
    {
        Post? post = catalog.FindPost(slug);
        Author? author = post is null ? null : catalog.FindAuthor(post.AuthorSlug);
        if (post is null || author is null)
        {
            return NotFound();
        }

        Post = post;
        Author = author;
        Lang = Request.Path.StartsWithSegments("/en") ? "en" : "tr";

        ViewData["Lang"] = Lang;
        ViewData["Title"] = post.Title.Get(Lang);
        ViewData["JsonLd"] = new SiteGraph("https://bookstore.example").Post(post, author, Lang);
        return Page();
    }
}
