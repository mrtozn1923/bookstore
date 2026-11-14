using Bookstore.Api.Books;
using Bookstore.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<BookstoreContext>("bookstoredb");
builder.AddRedisOutputCache("redis");

WebApplication app = builder.Build();

app.UseOutputCache();

app.MapDefaultEndpoints();
app.MapBookEndpoints();

app.Run();
