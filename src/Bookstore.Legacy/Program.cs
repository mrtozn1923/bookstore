// Stand-in for the old ASP.NET MVC 5 site: same URLs and the same kind of HTML responses,
// so the strangler fig example runs without .NET Framework.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Served-By"] = "legacy";
    await next();
});

app.MapGet("/", () => Page("Home"));
app.MapGet("/books", () => Page("Books"));
app.MapGet("/books/{isbn}", (string isbn) => Page($"Book {isbn}"));
app.MapGet("/cart", () => Page("Cart"));
app.MapGet("/account/login", () => Page("Log in"));

app.Run();

static IResult Page(string title) =>
    Results.Content($"<h1>{title} (legacy MVC 5)</h1>", "text/html");
