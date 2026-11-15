using System.Text.RegularExpressions;

namespace Bookstore.Analytics.Web.Analytics;

// First-party cookie written by the Angular app with the value of gtag('get', ..., 'client_id').
public static partial class ClientIdCookie
{
    public const string Name = "bs_ga_cid";

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex Valid();

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out string? value) && Valid().IsMatch(value) ? value : null;
}
