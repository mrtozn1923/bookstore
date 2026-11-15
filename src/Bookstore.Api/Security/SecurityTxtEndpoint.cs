namespace Bookstore.Security;

// RFC 9116: the file lives in the repo, is reviewed like code and is served as-is.
public static class SecurityTxtEndpoint
{
    public const string Path = "/.well-known/security.txt";

    public static IEndpointRouteBuilder MapSecurityTxt(this IEndpointRouteBuilder app)
    {
        IWebHostEnvironment env = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        string content = File.ReadAllText(System.IO.Path.Combine(env.ContentRootPath, "security.txt"));

        app.MapGet(Path, () => Results.Text(content, "text/plain; charset=utf-8"))
            .ExcludeFromDescription();
        return app;
    }
}
