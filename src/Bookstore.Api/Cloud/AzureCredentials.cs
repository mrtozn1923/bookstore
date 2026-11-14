using Azure.Core;
using Azure.Identity;

namespace Bookstore.Api.Cloud;

public static class AzureCredentials
{
    // Production'da yalnız platform kimliği; geliştirmede geliştiricinin kendi Azure oturumu
    public static TokenCredential Create(IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment())
        {
            return new ChainedTokenCredential(new AzureCliCredential(), new VisualStudioCredential());
        }

        // AKS Workload Identity webhook'u bu değişkeni pod'a ekler
        if (!string.IsNullOrEmpty(configuration["AZURE_FEDERATED_TOKEN_FILE"]))
        {
            return new WorkloadIdentityCredential();
        }

        return configuration["Azure:ManagedIdentityClientId"] is { Length: > 0 } clientId
            ? new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId))
            : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
    }
}
