// The new ASP.NET Core site that takes over www page by page.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Served-By"] = "web";
    await next();
});

app.MapRazorPages();

app.Run();
