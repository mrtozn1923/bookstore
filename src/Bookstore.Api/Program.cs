using Bookstore.Api.Ai;
using Bookstore.Api.Catalog;
using Bookstore.Api.Returns;
using Bookstore.Api.Support;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddClaude(builder.Configuration);
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
builder.Services.AddScoped<ClaudeCatalogService>();
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddScoped<SupportChatService>();
builder.Services.AddScoped<InvoiceAnalyzer>();
builder.Services.AddScoped<ReturnClassifier>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapCatalogEndpoints();
app.MapSupportEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapModelEndpoints();
}

app.Run();
