namespace Bookstore.Api;

// "dotnet Bookstore.Api.dll healthcheck": a probe for images that have no shell or curl
public static class HealthProbe
{
    public static async Task<int> RunAsync()
    {
        string port = (Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080").Split(';')[0];
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using HttpResponseMessage response = await http.GetAsync($"http://localhost:{port}/health");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
