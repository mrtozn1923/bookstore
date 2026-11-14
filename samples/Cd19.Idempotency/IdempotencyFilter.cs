using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.Api.Idempotency;

public sealed class IdempotencyFilter<TRequest>(BookstoreContext db, TimeProvider time) : IEndpointFilter
{
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        string? key = ParseKey(http.Request.Headers["Idempotency-Key"].ToString());
        if (key is null)
        {
            return Problem(400, "Idempotency-Key header is required",
                "Send a unique key (for example a UUID) with every POST /orders request.");
        }

        CurrentUser user = CurrentUser.From(http);
        TRequest request = context.Arguments.OfType<TRequest>().Single();
        byte[] hash = Fingerprint(http.Request.Method, http.Request.Path, request);
        DateTimeOffset now = time.GetUtcNow();

        IdempotencyRecord record = new()
        {
            InstitutionId = user.InstitutionId, UserId = user.UserId, Key = key, RequestHash = hash,
            Status = IdempotencyStatus.Started, LockedUntil = now + Lease, ExpiresAt = now + Retention
        };

        // step 1: claim the key (the unique index acts as the lock)
        if (!await TryClaimAsync(record, http.RequestAborted))
        {
            IdempotencyRecord existing = await db.IdempotencyRecords.AsNoTracking()
                .SingleAsync(r => r.InstitutionId == user.InstitutionId && r.UserId == user.UserId && r.Key == key,
                    http.RequestAborted);

            if (!existing.RequestHash.SequenceEqual(hash))
            {
                return Problem(422, "Idempotency-Key was used with a different payload",
                    "Use a new key for a new request.");
            }

            if (existing.Status == IdempotencyStatus.Completed)
            {
                return new ReplayResult(existing);
            }

            if (existing.LockedUntil > now || !await TryTakeOverAsync(existing.Id, now, http.RequestAborted))
            {
                return Problem(409, "A request with this Idempotency-Key is in progress",
                    "Retry after the original request completes.");
            }

            record = await db.IdempotencyRecords.SingleAsync(r => r.Id == existing.Id, http.RequestAborted);
        }

        // step 2: the work and the "completed" record in the same transaction
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(http.RequestAborted);
        Stream originalBody = http.Response.Body;
        using MemoryStream buffer = new();
        try
        {
            if (await next(context) is not IResult inner)
            {
                throw new InvalidOperationException("Idempotent endpoints must return IResult.");
            }

            http.Response.Body = buffer;
            await inner.ExecuteAsync(http);
        }
        catch
        {
            await tx.RollbackAsync();
            await ReleaseAsync(record);
            throw;
        }
        finally
        {
            http.Response.Body = originalBody;
        }

        if (http.Response.StatusCode is >= 200 and < 300)
        {
            record.Status = IdempotencyStatus.Completed;
            record.ResponseStatusCode = http.Response.StatusCode;
            record.ResponseContentType = http.Response.ContentType;
            record.ResponseLocation = http.Response.Headers.Location.ToString();
            record.ResponseBody = Encoding.UTF8.GetString(buffer.ToArray());
            await db.SaveChangesAsync(http.RequestAborted);
            await tx.CommitAsync(http.RequestAborted);
        }
        else
        {
            // validation and business rule errors are not cached: the key is released
            await tx.RollbackAsync(http.RequestAborted);
            await ReleaseAsync(record);
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody, http.RequestAborted);
        return Results.Empty;
    }

    private async Task<bool> TryClaimAsync(IdempotencyRecord record, CancellationToken ct)
    {
        db.IdempotencyRecords.Add(record);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(record).State = EntityState.Detached;
            return false;
        }
    }

    private async Task<bool> TryTakeOverAsync(long id, DateTimeOffset now, CancellationToken ct)
    {
        // take over a half-finished (crashed) request once its lease has expired
        int rows = await db.IdempotencyRecords
            .Where(r => r.Id == id && r.Status == IdempotencyStatus.Started && r.LockedUntil <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LockedUntil, now + Lease), ct);
        return rows == 1;
    }

    private async Task ReleaseAsync(IdempotencyRecord record)
    {
        db.ChangeTracker.Clear();
        await db.IdempotencyRecords.Where(r => r.Id == record.Id).ExecuteDeleteAsync();
    }

    internal static string? ParseKey(string raw)
    {
        // the draft wants an sf-string: "..."; we also accept clients that send it without quotes
        string key = raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"' ? raw[1..^1] : raw;
        return key.Length is >= 16 and <= 100 && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? key
            : null;
    }

    internal static byte[] Fingerprint(string method, string path, TRequest request)
    {
        string canonical = $"{method} {path} {JsonSerializer.Serialize(request, JsonSerializerOptions.Web)}";
        return SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
    }

    private static IResult Problem(int status, string title, string detail)
        => Results.Problem(statusCode: status, title: title, detail: detail);
}

public sealed class ReplayResult(IdempotencyRecord record) : IResult
{
    public async Task ExecuteAsync(HttpContext http)
    {
        http.Response.StatusCode = record.ResponseStatusCode!.Value;
        http.Response.ContentType = record.ResponseContentType;
        if (!string.IsNullOrEmpty(record.ResponseLocation))
        {
            http.Response.Headers.Location = record.ResponseLocation;
        }
        http.Response.Headers["Idempotent-Replayed"] = "true";
        await http.Response.WriteAsync(record.ResponseBody ?? "");
    }
}
