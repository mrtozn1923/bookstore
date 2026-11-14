using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bookstore.Api.Common;

// Idempotency-Key (draft-ietf-httpapi-idempotency-key-header-08) for POSTs that create something.
// Keys are scoped to the caller, live 24 hours, and are bound to a fingerprint of the request body.
public sealed partial class IdempotencyStore(TimeProvider time)
{
    public static readonly TimeSpan KeyLifetime = TimeSpan.FromHours(24);

    private sealed class Entry
    {
        public required string Fingerprint { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
        public object? Result { get; set; }
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    // Structured Field String: a quoted string of printable ASCII, at most 255 characters here.
    [GeneratedRegex("^\"[\\x20-\\x21\\x23-\\x5B\\x5D-\\x7E]{1,255}\"$")]
    private static partial Regex SfString();

    // TBody is the bound request body; its JSON is the fingerprint, so formatting differences do not matter.
    public async ValueTask<object?> InvokeAsync<TBody>(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        string? header = http.Request.Headers["Idempotency-Key"];
        if (header is null)
        {
            return ProblemCatalog.Create(ProblemCatalog.IdempotencyKeyMissing);
        }

        if (!SfString().IsMatch(header))
        {
            return Paging.Invalid("idempotency-key", "invalid-format");
        }

        TBody body = context.Arguments.OfType<TBody>().Single();
        string fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(body)));

        string scope = $"{http.User.Identity?.Name}|{http.Request.Method} {http.Request.Path}|{header}";
        DateTimeOffset now = time.GetUtcNow();
        Entry entry = new() { Fingerprint = fingerprint, ExpiresAt = now + KeyLifetime };
        Entry stored = _entries.AddOrUpdate(scope, entry, (_, old) => old.ExpiresAt <= now ? entry : old);
        if (!ReferenceEquals(stored, entry))
        {
            if (stored.Fingerprint != fingerprint)
            {
                return ProblemCatalog.Create(ProblemCatalog.IdempotencyKeyReused);
            }

            // Same key and payload: replay the first result, or 409 while it is still running.
            return stored.Result ?? ProblemCatalog.Create(ProblemCatalog.IdempotencyKeyInProgress);
        }

        try
        {
            object? result = await next(context);
            entry.Result = result;
            return result;
        }
        catch
        {
            // Nothing was committed: a retry with the same key runs again.
            _entries.TryRemove(scope, out _);
            throw;
        }
    }
}
