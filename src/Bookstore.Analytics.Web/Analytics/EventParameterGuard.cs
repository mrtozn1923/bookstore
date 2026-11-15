using System.Text.RegularExpressions;

namespace Bookstore.Analytics.Web.Analytics;

// Google's policy: nothing Google could recognise as personally identifiable information.
public static partial class EventParameterGuard
{
    private static readonly HashSet<string> ForbiddenKeys =
        new(["email", "e_mail", "name", "full_name", "phone", "user_name", "address"], StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"[^@\s]+@[^@\s]+\.[^@\s]+")]
    private static partial Regex EmailLike();

    public static void EnsureNoPersonalData(IReadOnlyDictionary<string, object> parameters)
    {
        foreach ((string key, object value) in parameters)
        {
            if (ForbiddenKeys.Contains(key) || value is string s && EmailLike().IsMatch(s))
            {
                throw new ArgumentException($"Event parameter '{key}' looks like personal data.", nameof(parameters));
            }
        }
    }
}
