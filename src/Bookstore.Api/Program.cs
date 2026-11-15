using Bookstore.Api.Ai;
using Bookstore.Api.Assistant;
using Bookstore.Api.Catalog;
using Bookstore.Api.Catalog.Publishers;
using Bookstore.Api.Catalog.Seo;
using Bookstore.Api.Ordering;
using Bookstore.Api.Returns;
using Bookstore.Api.Support;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddClaude(builder.Configuration);
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
builder.Services.AddScoped<ClaudeCatalogService>();
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddSingleton(SupportKnowledge.LoadEmbedded());
builder.Services.AddScoped<SupportChatService>();
builder.Services.AddScoped<InvoiceAnalyzer>();
builder.Services.AddScoped<ReturnClassifier>();
builder.Services.AddScoped<ShoppingAssistant>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<BookstoreTools>();
builder.Services.AddScoped<AssistantAgent>();
builder.Services.AddScoped<ToolRunnerAssistant>();
builder.Services.AddScoped<ReturnRequestExtractor>();
builder.Services.AddScoped<ReturnPolicy>();
builder.Services.AddSingleton<IBookSeoStore, InMemoryBookSeoStore>();
builder.Services.AddSingleton<ISeoBatchJobStore, InMemorySeoBatchJobStore>();
builder.Services.AddScoped<SeoBatchProcessor>();
builder.Services.AddOptions<SeoBatchOptions>().Bind(builder.Configuration.GetSection(SeoBatchOptions.SectionName));
builder.Services.AddHostedService<SeoBatchWorker>();
builder.Services.AddSingleton<IPublisherCatalogStore, InMemoryPublisherCatalogStore>();
builder.Services.AddScoped<PublisherCatalogService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapCatalogEndpoints();
app.MapSupportEndpoints();
app.MapAssistantEndpoints();
app.MapReturnEndpoints();
app.MapPublisherCatalogEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapModelEndpoints();
}

app.Run();
