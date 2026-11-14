using System.Buffers.Text;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Common;

public sealed record Page<T>(IReadOnlyList<T> Items, string? NextPageToken);

// First version of pagination (BS-30): the page token is base64url("offset=N"). Post 05 replaces it.
public static class Paging
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public static Results<Ok<Page<T>>, ProblemHttpResult> Paginate<T>(
        IReadOnlyList<T> source, int? pageSize, string? pageToken)
    {
        int size = Math.Min(pageSize ?? DefaultPageSize, MaxPageSize);
        if (size < 1)
        {
            return TypedResults.Problem(statusCode: 400, title: "Invalid page size",
                detail: $"pageSize must be between 1 and {MaxPageSize}.");
        }

        int offset = 0;
        if (pageToken is not null)
        {
            string decoded = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(pageToken));
            offset = int.Parse(decoded["offset=".Length..]);
        }

        List<T> items = [.. source.Skip(offset).Take(size)];
        string? next = offset + size < source.Count
            ? Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"offset={offset + size}"))
            : null;
        return TypedResults.Ok(new Page<T>(items, next));
    }
}
