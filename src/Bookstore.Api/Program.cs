using Bookstore.Api.Books;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<BookCatalog>();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapBookEndpoints();

app.Run();
