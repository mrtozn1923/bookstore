using Bookstore.Notifications.Api;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Options;

if (args is ["generate-vapid-keys"])
{
    // run once per environment and keep the pair in a secret store
    VapidKeys keys = VapidKeys.Generate();
    Console.WriteLine($"Push__PublicKey={keys.PublicKey}");
    Console.WriteLine($"Push__PrivateKey={keys.PrivateKey}");
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<PushOptions>()
    .Bind(builder.Configuration.GetSection(PushOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<PushSubscriptionStore>();
builder.Services.AddHttpClient<PushServiceClient>();
builder.Services.AddTransient<OrderNotifier>();

WebApplication app = builder.Build();

// no sign-in in this series: every request belongs to the customer Ada
const string customer = "ada";

app.MapGet("/api/push/public-key", (IOptions<PushOptions> options) =>
    TypedResults.Ok(new { publicKey = options.Value.PublicKey }));

app.MapPost("/api/push/subscriptions", (PushSubscription subscription, PushSubscriptionStore store, IOptions<PushOptions> options) =>
{
    // the endpoint comes from the client: never POST to an arbitrary URL (SSRF)
    if (!Uri.TryCreate(subscription.Endpoint, UriKind.Absolute, out Uri? endpoint) || !options.Value.IsAllowed(endpoint))
        return Results.Problem("Unknown push service.", statusCode: StatusCodes.Status400BadRequest);
    if (subscription.Keys is null || !subscription.Keys.ContainsKey("p256dh") || !subscription.Keys.ContainsKey("auth"))
        return Results.Problem("The subscription has no keys.", statusCode: StatusCodes.Status400BadRequest);
    store.Save(customer, subscription);
    return Results.Created();
});

app.MapDelete("/api/push/subscriptions", (string endpoint, PushSubscriptionStore store) =>
    store.Remove(endpoint) ? Results.NoContent() : Results.NotFound());

// in Bookstore the Shipping module raises this; here a request stands in for it
app.MapPost("/api/orders/{orderId:int}/shipped", async (int orderId, OrderNotifier notifier, CancellationToken ct) =>
    TypedResults.Ok(await notifier.OrderShippedAsync(customer, orderId, ct)));

app.Run();
