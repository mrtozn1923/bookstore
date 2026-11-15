using Google.Apis.Auth.OAuth2;
using Google.Apis.SearchConsole.v1;
using Google.Apis.Services;

namespace Bookstore.Analytics.SearchConsole;

public static class SearchConsoleClient
{
    // The key file lives outside the repository; its path comes from configuration.
    public static async Task<SearchConsoleService> CreateAsync(string keyFilePath, CancellationToken ct = default)
    {
        ServiceAccountCredential account =
            await CredentialFactory.FromFileAsync<ServiceAccountCredential>(keyFilePath, ct);
        GoogleCredential credential = account.ToGoogleCredential()
            .CreateScoped(SearchConsoleService.ScopeConstants.WebmastersReadonly);

        return new SearchConsoleService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "bookstore-analytics",
        });
    }

    // Local runs against a stub server; no credentials involved.
    public static SearchConsoleService CreateForStub(Uri baseUri) =>
        new(new BaseClientService.Initializer
        {
            BaseUri = baseUri.ToString(),
            ApplicationName = "bookstore-analytics",
        });
}
