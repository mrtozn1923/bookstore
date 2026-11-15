using Bookstore.Api.Ai;
using Bookstore.Api.Catalog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddClaude(builder.Configuration);
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
builder.Services.AddScoped<ClaudeCatalogService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapCatalogEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapModelEndpoints();
}

app.Run();
