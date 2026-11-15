WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

string[] locales = ["tr", "en"];

// "/" → the visitor's language, Turkish by default
app.MapGet("/", (HttpRequest request) =>
{
    string first = request.Headers.AcceptLanguage.ToString().Split(',')[0];
    string locale = first.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";
    return Results.Redirect($"/{locale}/");
});

app.UseStaticFiles();

// /tr/books/9780441172719 → tr/index.html, so a refresh does not end in 404
foreach (string locale in locales)
{
    app.MapFallbackToFile($"/{locale}/{{**path:nonfile}}", $"{locale}/index.html");
}

app.Run();
