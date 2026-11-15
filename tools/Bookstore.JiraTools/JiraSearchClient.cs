using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bookstore.JiraTools;

public sealed class JiraSearchClient(HttpClient http)
{
    private const string Fields = "summary,issuetype";

    public static HttpClient CreateHttpClient(Uri siteUrl, string email, string apiToken)
    {
        HttpClient http = new() { BaseAddress = siteUrl };
        string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }

    public static string BuildSearchPath(string jql, string? nextPageToken = null)
    {
        string path = $"rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&fields={Fields}&maxResults=100";
        return nextPageToken is null ? path : $"{path}&nextPageToken={Uri.EscapeDataString(nextPageToken)}";
    }

    public async Task<IReadOnlyList<WorkItem>> SearchAsync(string jql, CancellationToken cancellationToken = default)
    {
        List<WorkItem> items = [];
        string? nextPageToken = null;
        do
        {
            using HttpResponseMessage response = await http.GetAsync(BuildSearchPath(jql, nextPageToken), cancellationToken);
            response.EnsureSuccessStatusCode();
            await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            JsonElement root = json.RootElement;

            foreach (JsonElement issue in root.GetProperty("issues").EnumerateArray())
            {
                JsonElement fields = issue.GetProperty("fields");
                items.Add(new WorkItem(
                    issue.GetProperty("key").GetString()!,
                    fields.GetProperty("issuetype").GetProperty("name").GetString()!,
                    fields.GetProperty("summary").GetString()!));
            }

            bool isLast = root.TryGetProperty("isLast", out JsonElement last) && last.GetBoolean();
            nextPageToken = !isLast && root.TryGetProperty("nextPageToken", out JsonElement token) ? token.GetString() : null;
        }
        while (nextPageToken is not null);
        return items;
    }
}
