using System.Security.Cryptography;
using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record VerifyRequest(string Code);

public static class VerificationEndpoints
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);

    public static IEndpointRouteBuilder MapVerification(this IEndpointRouteBuilder app)
    {
        app.MapPost("/customers/{customerId:int}/verification-code",
            async (int customerId, IConnectionMultiplexer redis, ILoggerFactory loggers) =>
        {
            string code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
            // A new code replaces the old one, together with a fresh 5-minute TTL
            await redis.GetDatabase().StringSetAsync(RedisKeys.VerificationCode(customerId), code, CodeLifetime);
            loggers.CreateLogger("Bookstore.Verification").LogInformation("Verification code for customer {CustomerId}: {Code} (would be e-mailed)", customerId, code);
            return Results.Accepted(value: new { expiresInSeconds = (int)CodeLifetime.TotalSeconds });
        });

        app.MapPost("/customers/{customerId:int}/verification-code/verify",
            async (int customerId, VerifyRequest request, IConnectionMultiplexer redis) =>
        {
            // DELEX key IFEQ code: deletes only if the stored code matches; one atomic step (Redis 8.4+)
            bool verified = await redis.GetDatabase()
                .StringDeleteAsync(RedisKeys.VerificationCode(customerId), ValueCondition.Equal(request.Code));
            return verified
                ? Results.Ok(new { verified })
                : Results.BadRequest(new { verified, error = "invalid or expired code" });
        });

        return app;
    }
}
