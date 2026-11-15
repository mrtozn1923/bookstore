using System.Text.Json;

namespace Bookstore.Web.State;

public static class SessionJsonExtensions
{
    public static T? GetJson<T>(this ISession session, string key) =>
        session.GetString(key) is { } json ? JsonSerializer.Deserialize<T>(json) : default;

    public static void SetJson<T>(this ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));
}
