using Bookstore.Api.Agents;
using Bookstore.Api.Ai;
using Bookstore.Api.Assistant;
using Bookstore.Api.Catalog;
using Bookstore.Api.Catalog.Publishers;
using Bookstore.Api.Catalog.Seo;
using Bookstore.Api.Ordering;
using Bookstore.Api.Reports;
using Bookstore.Api.Returns;
using Bookstore.Api.Support;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddClaude(builder.Configuration);
builder.Services.AddExceptionHandler<ClaudeExceptionHandler>();
builder.Services.AddSingleton<ClaudeUsageMeter>();
builder.Services.AddClaudeChatClient();
// Spans and metrics go to an OTLP collector only when one is configured (OTEL_EXPORTER_OTLP_ENDPOINT).
bool exportOtlp = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("com.anthropic.sdk.csharp*").AddSource("Bookstore.ChatClient").AddAspNetCoreInstrumentation();
        if (exportOtlp)
        {
            tracing.AddOtlpExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(ClaudeUsageMeter.MeterName);
        if (exportOtlp)
        {
            metrics.AddOtlpExporter();
        }
    });
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
builder.Services.AddOptions<SalesReportOptions>().Bind(builder.Configuration.GetSection(SalesReportOptions.SectionName));
builder.Services.AddScoped<SalesReportService>();
builder.Services.AddSingleton<IAgentRegistry, InMemoryAgentRegistry>();
builder.Services.AddScoped<IssueDigestAgent>();

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
    app.MapReportEndpoints();
}

app.Run();
