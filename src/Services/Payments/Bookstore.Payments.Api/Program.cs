using Bookstore.Contracts;
using Bookstore.Contracts.Payments;
using Bookstore.Payments.Data;
using Bookstore.Payments.Provider;
using Bookstore.Payments.Reconciliation;
using Bookstore.Payments.Webhooks;
using Microsoft.Extensions.Http.Resilience;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Persistence;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// Same reasoning as the other services: the outbox needs its own transaction, the migrator keeps EF Core's retries.
builder.AddSqlServerDbContext<PaymentsDbContext>("paymentsdb", settings => settings.DisableRetry = !DatabaseMigration.IsMigrationRun(args));

builder.UseWolverine(opts =>
{
    opts.PersistMessagesWithSqlServerUsingNamedConnection("paymentsdb", "wolverine");
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();
    // Lightweight: no database transaction is held open while the handler waits for the provider's HTTP answer;
    // SaveChanges at the end writes the payment and the outgoing messages together.
    opts.UseEntityFrameworkCoreTransactions(TransactionMiddlewareMode.Lightweight);
    opts.Policies.AutoApplyTransactions();
    // A typed HttpClient is created by IHttpClientFactory through a factory lambda, which Wolverine 6
    // refuses to resolve by default (ServiceLocationPolicy.NotAllowed). Allow it for this one type.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<PaymentProviderClient>();

    opts.ListenToRabbitQueue(CommandQueues.Payments)
        .UseDurableInbox()
        .DeadLetterQueueing(new DeadLetterQueue("payments.dead-letter"));
    opts.PublishMessage<PaymentSucceeded>().ToRabbitExchange(Exchanges.Payments);
    opts.PublishMessage<PaymentFailed>().ToRabbitExchange(Exchanges.Payments);
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // The provider is down or answers 5xx: try the message again later. Safe, because every call carries
    // an Idempotency-Key derived from the order.
    opts.OnException<HttpRequestException>().RetryWithCooldown(
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WebhookSignature>();
builder.Services.Configure<ReconciliationOptions>(builder.Configuration.GetSection("Reconciliation"));
builder.Services.AddHostedService<PaymentReconciliationJob>();

// ServiceDefaults gives every client the standard handler, whose retry repeats a POST after a timeout.
// For a charge, repeating is the message handler's decision (with the same Idempotency-Key), not the HTTP layer's.
// RemoveAllResilienceHandlers is still marked experimental in Microsoft.Extensions.Http.Resilience 10.11.
#pragma warning disable EXTEXP0001
builder.Services.AddHttpClient<PaymentProviderClient>(client => client.BaseAddress = new Uri("https+http://payment-provider"))
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.DisableForUnsafeHttpMethods();
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
    });
#pragma warning restore EXTEXP0001

WebApplication app = builder.Build();

if (DatabaseMigration.IsMigrationRun(args))
{
    await app.MigrateDatabaseAsync();
    return;
}

app.MapDefaultEndpoints();
app.MapProviderWebhook();
app.MapGet("/", () => "Bookstore.Payments.Api");

app.Run();
