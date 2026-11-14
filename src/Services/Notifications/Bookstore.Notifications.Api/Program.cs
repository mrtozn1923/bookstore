using System.Net.Sockets;
using Bookstore.Contracts;
using Bookstore.Notifications.Email;
using Bookstore.Notifications.Live;
using Microsoft.AspNetCore.SignalR;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRedisClient("redis");

builder.UseWolverine(opts =>
{
    // No database: Notifications has no inbox table. Its queue uses RabbitMQ's own dead letter queue.
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .DeclareExchange(Exchanges.Ordering, exchange => exchange.BindQueue("notifications.order-events"));
    opts.ListenToRabbitQueue("notifications.order-events")
        .DeadLetterQueueing(new DeadLetterQueue("notifications.dead-letter"));

    // The SMTP server is down or refuses: try again later instead of losing the e-mail at once.
    opts.OnException<SocketException>().Or<MailKit.ProtocolException>().Or<MailKit.Net.Smtp.SmtpCommandException>()
        .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IOrderEmails, OrderEmails>();

// The hub accepts the same access tokens as the other services; the BFF forwards the user's token.
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "bookstore", options =>
    {
        options.Audience = "bookstore-api";
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
    });
builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapHub<OrderStatusHub>("/hubs/orders");

app.Run();
