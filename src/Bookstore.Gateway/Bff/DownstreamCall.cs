using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace Bookstore.Gateway.Bff;

public sealed record DownstreamResult<T>(T? Value, HttpStatusCode? Status, string? Error, long ElapsedMs);

public static class DownstreamCall
{
    // Never throws for downstream problems: the caller decides whether a failure breaks the page.
    public static async Task<DownstreamResult<T>> GetAsync<T>(
        HttpClient client, string path, AuthenticationHeaderValue? authorization, CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, path);
            request.Headers.Authorization = authorization;
            using HttpResponseMessage response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new(default, response.StatusCode, $"status {(int)response.StatusCode}", sw.ElapsedMilliseconds);
            }
            T? value = await response.Content.ReadFromJsonAsync<T>(ct);
            return new(value, response.StatusCode, null, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new(default, null, "timeout", sw.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            return new(default, null, ex.HttpRequestError.ToString(), sw.ElapsedMilliseconds);
        }
    }
}
