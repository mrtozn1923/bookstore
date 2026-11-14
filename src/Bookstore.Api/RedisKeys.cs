using StackExchange.Redis;

namespace Bookstore.Api;

// Bookstore key naming: bookstore:<entity>:<id>[:<detail>]
public static class RedisKeys
{
    private const string Prefix = "bookstore";

    public static RedisKey BookViews(int bookId) => $"{Prefix}:book:{bookId}:views";
}
