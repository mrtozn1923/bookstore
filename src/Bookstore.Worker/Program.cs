using Bookstore.Worker;
using StackExchange.Redis;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is missing.");
IConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(redisConnection);
builder.Services.AddSingleton(redis);

// Two consumer groups on the same stream: each group gets every event once
builder.Services.AddHostedService<EmailConsumer>();
builder.Services.AddHostedService<StockConsumer>();

await builder.Build().RunAsync();
