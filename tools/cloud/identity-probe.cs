// Asks Microsoft Entra ID for a Storage token the way Bookstore.Api does and prints what happened.
// Run: dotnet run tools/cloud/identity-probe.cs -- [default|managed]
#:package Azure.Identity

using Azure.Core;
using Azure.Identity;

string mode = args.Length > 0 ? args[0] : "default";
string? clientId = Environment.GetEnvironmentVariable("BOOKSTORE_IDENTITY_CLIENT_ID");

TokenCredential credential = mode == "managed"
    ? new ManagedIdentityCredential(clientId is null ? ManagedIdentityId.SystemAssigned : ManagedIdentityId.FromUserAssignedClientId(clientId))
    : new DefaultAzureCredential();

TokenRequestContext request = new(["https://storage.azure.com/.default"]);
try
{
    AccessToken token = await credential.GetTokenAsync(request, CancellationToken.None);
    Console.WriteLine($"{credential.GetType().Name}: token received, {token.Token.Length} chars, expires {token.ExpiresOn:u}");
}
catch (AuthenticationFailedException ex)
{
    Console.WriteLine($"{credential.GetType().Name} failed:");
    Console.WriteLine(ex.Message);
}
