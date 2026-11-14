using Bookstore.Contracts;
using Bookstore.Contracts.Payments;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.UseWolverine(opts =>
{
    // Inbox and outbox in paymentsdb; post 08 adds Payments' own tables next to them.
    opts.PersistMessagesWithSqlServerUsingNamedConnection("paymentsdb", "wolverine");
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

    opts.ListenToRabbitQueue(CommandQueues.Payments)
        .UseDurableInbox()
        .DeadLetterQueueing(new DeadLetterQueue("payments.dead-letter"));
    opts.PublishMessage<PaymentSucceeded>().ToRabbitExchange(Exchanges.Payments);
    opts.PublishMessage<PaymentFailed>().ToRabbitExchange(Exchanges.Payments);
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => "Bookstore.Payments.Api");

app.Run();
