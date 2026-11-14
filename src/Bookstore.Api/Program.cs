using Bookstore.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseRequestTiming();

app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("X-Maintenance"))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsync("Bookstore is under maintenance");
        return;
    }

    await next(context);
});

app.Map("/ping", branch =>
{
    branch.Run(async context => await context.Response.WriteAsync("pong"));
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.Map("/error", () => Results.Problem());

app.Run();
