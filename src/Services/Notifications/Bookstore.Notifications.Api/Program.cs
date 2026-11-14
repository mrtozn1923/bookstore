using Bookstore.Contracts;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.UseWolverine(opts =>
{
    // No database: Notifications has no inbox table. Its queue uses RabbitMQ's own dead letter queue.
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .DeclareExchange(Exchanges.Ordering, exchange => exchange.BindQueue("notifications.order-events"));
    opts.ListenToRabbitQueue("notifications.order-events")
        .DeadLetterQueueing(new DeadLetterQueue("notifications.dead-letter"));
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Bookstore.Notifications.Api");

app.Run();
