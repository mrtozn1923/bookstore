using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;

namespace Bookstore.Api.Common;

public sealed record Page<T>(IReadOnlyList<T> Items, string? NextPageToken);

// A field a collection can be sorted by. Key returns a string, decimal or DateTime.
public sealed record SortField<T>(string Name, Func<T, object> Key);

public sealed record SortSpec<T>(SortField<T> Field, bool Descending);

// What the page token carries: the query it belongs to and the sort key of the last item returned.
internal sealed record PageTokenPayload(string Query, string[] LastKey);

// Pagination contract (BS-30..BS-36): pageSize 0 or absent = 10, above 50 = 50, negative = 400;
// nextPageToken is opaque (encrypted, signed, 3 days), bound to the filters and sort of the first request.
public sealed class Paging(IDataProtectionProvider protection)
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(3);

    private readonly ITimeLimitedDataProtector _protector =
        protection.CreateProtector("Bookstore.PageToken.v1").ToTimeLimitedDataProtector();

    public static ProblemHttpResult? ParseSort<T>(
        string? sort, IReadOnlyList<SortField<T>> fields, out List<SortSpec<T>> specs)
    {
        specs = [];
        foreach (string part in (sort ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            bool descending = part.StartsWith('-');
            string name = part.TrimStart('-');
            if (fields.FirstOrDefault(f => f.Name == name) is not { } field)
            {
                return Invalid("sort", "unknown-field");
            }

            specs.Add(new SortSpec<T>(field, descending));
        }

        return null;
    }

    public static ProblemHttpResult Invalid(string key, string code) =>
        TypedResults.Problem(new HttpValidationProblemDetails(
            new Dictionary<string, string[]> { [key] = [code] }) { Status = 400 });

    // Unknown query parameters are errors, so a typo such as ?catgory= does not silently return everything.
    public static ProblemHttpResult? RejectUnknownParameters(HttpRequest request, IReadOnlyCollection<string> allowed) =>
        request.Query.Keys.FirstOrDefault(k => !allowed.Contains(k)) is { } unknown
            ? Invalid(unknown, "unknown-parameter")
            : null;

    public Results<Ok<Page<TOut>>, ProblemHttpResult> Paginate<T, TOut>(
        HttpContext http, IEnumerable<T> source, IReadOnlyList<SortSpec<T>> sort, SortField<T> tieBreaker,
        string query, int? pageSize, string? pageToken, Func<T, TOut> map)
    {
        if (pageSize < 0)
        {
            return Invalid("pageSize", "out-of-range");
        }

        int size = pageSize is null or 0 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);
        List<SortSpec<T>> order = [.. sort, new SortSpec<T>(tieBreaker, Descending: false)];
        string fingerprint = Fingerprint(query + "|" + string.Join(',', order.Select(s => (s.Descending ? "-" : "") + s.Field.Name)));
        List<T> sorted = [.. source.Order(Comparer<T>.Create((a, b) => Compare(order, a, b)))];

        int start = 0;
        if (pageToken is not null)
        {
            if (Read(pageToken) is not { } payload || payload.Query != fingerprint)
            {
                return ProblemCatalog.Create(ProblemCatalog.InvalidPageToken);
            }

            // Keyset: continue after the last key, so inserts and deletes do not shift the pages.
            start = sorted.FindIndex(item => CompareToKey(order, item, payload.LastKey) > 0);
            start = start < 0 ? sorted.Count : start;
        }

        List<T> pageItems = [.. sorted.Skip(start).Take(size)];
        string? next = null;
        if (start + pageItems.Count < sorted.Count)
        {
            string[] lastKey = [.. order.Select(s => Format(s.Field.Key(pageItems[^1])))];
            next = _protector.Protect(JsonSerializer.Serialize(new PageTokenPayload(fingerprint, lastKey)), TokenLifetime);
            Dictionary<string, string?> nextQuery = http.Request.Query
                .Where(q => q.Key != "pageToken")
                .ToDictionary(q => q.Key, q => (string?)q.Value.ToString());
            nextQuery["pageToken"] = next;
            http.Response.Headers.Link = $"<{QueryHelpers.AddQueryString(http.Request.Path, nextQuery)}>; rel=\"next\"";
        }

        return TypedResults.Ok(new Page<TOut>([.. pageItems.Select(map)], next));
    }

    private PageTokenPayload? Read(string token)
    {
        try
        {
            return JsonSerializer.Deserialize<PageTokenPayload>(_protector.Unprotect(token, out _));
        }
        catch (Exception e) when (e is CryptographicException or FormatException or JsonException)
        {
            return null;
        }
    }

    private static int Compare<T>(List<SortSpec<T>> order, T a, T b)
    {
        foreach (SortSpec<T> spec in order)
        {
            int result = CompareValues(spec.Field.Key(a), spec.Field.Key(b));
            if (result != 0)
            {
                return spec.Descending ? -result : result;
            }
        }

        return 0;
    }

    private static int CompareToKey<T>(List<SortSpec<T>> order, T item, string[] key)
    {
        for (int i = 0; i < order.Count; i++)
        {
            object value = order[i].Field.Key(item);
            int result = CompareValues(value, Parse(key[i], value.GetType()));
            if (result != 0)
            {
                return order[i].Descending ? -result : result;
            }
        }

        return 0;
    }

    private static int CompareValues(object a, object b) => a is string s
        ? StringComparer.OrdinalIgnoreCase.Compare(s, (string)b)
        : Comparer<object>.Default.Compare(a, b);

    private static string Format(object value) => value switch
    {
        DateTime d => d.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static object Parse(string text, Type type) =>
        type == typeof(decimal) ? decimal.Parse(text, CultureInfo.InvariantCulture)
        : type == typeof(DateTime) ? DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        : text;

    private static string Fingerprint(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
}
