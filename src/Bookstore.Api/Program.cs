using Bookstore.Api;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// One multiplexer for the whole application, shared by every request
string redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is missing.");
IConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(redisConnection);
builder.Services.AddSingleton(redis);

WebApplication app = builder.Build();

app.MapBookViews();

app.Run();
